using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CareHub.Desktop.Services;
using Microsoft.Win32;

namespace CareHub.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CareHubSession _session = new();

    [ObservableProperty] private string _statusMessage = "Sign in to continue.";
    [ObservableProperty] private string _loginPhone = "";
    [ObservableProperty] private string _loginPassword = "";
    [ObservableProperty] private bool _isSignedIn;

    [ObservableProperty] private string _patientSearchQuery = "";
    public ObservableCollection<PatientRow> Patients { get; } = new();

    [ObservableProperty] private string _newFirst = "";
    [ObservableProperty] private string _newLast = "";
    [ObservableProperty] private string _newPhone = "";
    [ObservableProperty] private string _newEmail = "";
    [ObservableProperty] private DateTime _newDob = DateTime.Today.AddYears(-30);

    [ObservableProperty] private string _bookPatientSearch = "";
    [ObservableProperty] private string _bookDoctorSearch = "";
    [ObservableProperty] private string _selectedSpecialty = "All";
    [ObservableProperty] private DateTime _bookDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private PatientRow? _selectedBookPatient;
    [ObservableProperty] private DoctorRow? _selectedBookDoctor;
    [ObservableProperty] private string? _selectedSlotTime;
    /// <summary>Local date/time; user can still override manually.</summary>
    [ObservableProperty] private string _bookScheduledAtText = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:mm");
    [ObservableProperty] private int _bookDuration = 30;
    [ObservableProperty] private string _branchCaption = "Main clinic";
    [ObservableProperty] private string _branchHint = "Appointments are created in the doctor's branch.";

    [ObservableProperty] private string _documentId = "";
    [ObservableProperty] private bool _showLabTab;
    public ObservableCollection<LabRow> LabOrders { get; } = new();
    public ObservableCollection<PatientRow> BookPatients { get; } = new();
    public ObservableCollection<DoctorRow> BookDoctors { get; } = new();
    public ObservableCollection<string> SpecialtyOptions { get; } = new(["All"]);
    public ObservableCollection<string> AvailableSlots { get; } = new();

    public ICollectionView BookPatientsView { get; }
    public ICollectionView BookDoctorsView { get; }

    public MainViewModel()
    {
        BookPatientsView = CollectionViewSource.GetDefaultView(BookPatients);
        BookPatientsView.Filter = FilterPatient;
        BookDoctorsView = CollectionViewSource.GetDefaultView(BookDoctors);
        BookDoctorsView.Filter = FilterDoctor;
        _ = TryStartupRefreshAsync();
    }

    private async Task TryStartupRefreshAsync()
    {
        try
        {
            if (await _session.TrySilentRefreshAsync(CancellationToken.None))
            {
                IsSignedIn = true;
                StatusMessage = "Restored session.";
                RefreshRoleFlags();
                await SearchPatientsAsync(CancellationToken.None);
                await LoadDoctorsAsync(CancellationToken.None);
            }
        }
        catch
        {
            /* ignore */
        }
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken ct)
    {
        try
        {
            await _session.LoginAsync(LoginPhone, LoginPassword, ct);
            IsSignedIn = true;
            StatusMessage = "Signed in.";
            RefreshRoleFlags();
            await SearchPatientsAsync(ct);
            await LoadDoctorsAsync(ct);
        }
        catch (Exception ex)
        {
            StatusMessage = "Login failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _session.Logout();
        IsSignedIn = false;
        Patients.Clear();
        LabOrders.Clear();
        StatusMessage = "Signed out.";
        ShowLabTab = false;
    }

    private void RefreshRoleFlags()
    {
        ShowLabTab = _session.RolesFromToken().Any(r => r is "LabTechnician" or "Admin" or "Manager");
    }

    [RelayCommand]
    private async Task SearchPatientsAsync(CancellationToken ct)
    {
        try
        {
            var q = string.IsNullOrWhiteSpace(PatientSearchQuery)
                ? ""
                : $"?q={Uri.EscapeDataString(PatientSearchQuery.Trim())}";
            var json = await _session.GetJsonAsync("/api/patients" + q, ct);
            var rows = _session.Deserialize<List<PatientRow>>(json) ?? [];
            Patients.Clear();
            foreach (var r in rows)
                Patients.Add(r);
            BookPatients.Clear();
            foreach (var r in rows)
                BookPatients.Add(r);
            BookPatientsView.Refresh();
            StatusMessage = $"Found {Patients.Count} patient(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = "Search failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task RegisterPatientAsync(CancellationToken ct)
    {
        try
        {
            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                firstName = NewFirst,
                lastName = NewLast,
                phoneNumber = NewPhone,
                email = string.IsNullOrWhiteSpace(NewEmail) ? null : NewEmail,
                dateOfBirth = DateOnly.FromDateTime(NewDob).ToString("yyyy-MM-dd"),
            });
            var json = await _session.PostJsonAsync("/api/patients", body, ct);
            var created = _session.Deserialize<PatientRow>(json);
            StatusMessage = created is null
                ? "Patient created."
                : $"{created.FirstName} {created.LastName} created.";
            await SearchPatientsAsync(ct);
        }
        catch (Exception ex)
        {
            StatusMessage = "Registration failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task BookAppointmentAsync(CancellationToken ct)
    {
        try
        {
            if (SelectedBookPatient is null || SelectedBookDoctor is null)
            {
                StatusMessage = "Select patient and doctor first.";
                return;
            }

            if (!DateTime.TryParse(BookScheduledAtText, out var localWhen))
            {
                StatusMessage = "Could not parse scheduled date/time.";
                return;
            }

            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                patientId = SelectedBookPatient.Id,
                doctorId = SelectedBookDoctor.Id,
                branchId = SelectedBookDoctor.BranchId,
                scheduledAt = localWhen.ToString("yyyy-MM-ddTHH:mm:ss"),
                durationMinutes = BookDuration,
            });
            await _session.PostJsonAsync("/api/appointments", body, ct);
            StatusMessage = "Appointment booked.";
            await LoadSlotsAsync(ct);
        }
        catch (Exception ex)
        {
            StatusMessage = "Booking failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadDoctorsAsync(CancellationToken ct)
    {
        try
        {
            var json = await _session.GetJsonAsync("/api/doctors", ct);
            var rows = _session.Deserialize<List<DoctorRow>>(json) ?? [];
            BookDoctors.Clear();
            foreach (var r in rows.Where(x => x.IsActive))
                BookDoctors.Add(r);

            var specialties = rows.Select(x => x.Specialty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x);
            SpecialtyOptions.Clear();
            SpecialtyOptions.Add("All");
            foreach (var s in specialties)
                SpecialtyOptions.Add(s!);

            BookDoctorsView.Refresh();
            UpdateBranchLabel();
            await LoadSlotsAsync(ct);
        }
        catch (Exception ex)
        {
            StatusMessage = "Loading doctors failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadSlotsAsync(CancellationToken ct)
    {
        if (SelectedBookDoctor is null)
        {
            AvailableSlots.Clear();
            return;
        }

        try
        {
            var date = DateOnly.FromDateTime(BookDate).ToString("yyyy-MM-dd");
            var json = await _session.GetJsonAsync($"/api/doctors/{SelectedBookDoctor.Id}/slots?date={Uri.EscapeDataString(date)}", ct);
            var rows = _session.Deserialize<List<SlotRow>>(json) ?? [];
            AvailableSlots.Clear();
            foreach (var slot in rows.Select(x => x.SlotTime).Where(x => !string.IsNullOrWhiteSpace(x)))
                AvailableSlots.Add(slot!);

            if (AvailableSlots.Count > 0)
                SelectedSlotTime = AvailableSlots[0];
            else
                SelectedSlotTime = null;
        }
        catch (Exception ex)
        {
            StatusMessage = "Loading slots failed: " + ex.Message;
        }
    }

    partial void OnBookPatientSearchChanged(string value) => BookPatientsView.Refresh();
    partial void OnBookDoctorSearchChanged(string value) => BookDoctorsView.Refresh();
    partial void OnSelectedSpecialtyChanged(string value) => BookDoctorsView.Refresh();

    partial void OnSelectedBookDoctorChanged(DoctorRow? value)
    {
        UpdateBranchLabel();
        _ = LoadSlotsAsync(CancellationToken.None);
    }

    partial void OnBookDateChanged(DateTime value) => _ = LoadSlotsAsync(CancellationToken.None);

    partial void OnSelectedSlotTimeChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        BookScheduledAtText = $"{BookDate:yyyy-MM-dd}T{value}";
    }

    private bool FilterPatient(object obj)
    {
        if (obj is not PatientRow p) return false;
        if (string.IsNullOrWhiteSpace(BookPatientSearch)) return true;
        var q = BookPatientSearch.Trim();
        return $"{p.FirstName} {p.LastName} {p.PhoneNumber}"
            .Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterDoctor(object obj)
    {
        if (obj is not DoctorRow d) return false;
        if (!string.Equals(SelectedSpecialty, "All", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(SelectedSpecialty, d.Specialty, StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(BookDoctorSearch)) return true;
        var q = BookDoctorSearch.Trim();
        return $"{d.FirstName} {d.LastName} {d.Specialty}".Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateBranchLabel()
    {
        if (SelectedBookDoctor is null)
        {
            BranchCaption = "Main clinic";
            BranchHint = "Choose doctor to determine branch.";
            return;
        }

        BranchCaption = "Clinic branch";
        BranchHint = $"{SelectedBookDoctor.BranchId} (auto from doctor)";
    }

    [RelayCommand]
    private async Task CheckInAsync(PatientRow? row, CancellationToken ct)
    {
        if (row is null) return;
        try
        {
            var json = await _session.GetJsonAsync("/api/appointments", ct);
            var list = _session.Deserialize<List<AppointmentRow>>(json) ?? [];
            var appt = list.FirstOrDefault(a => a.PatientId == row.Id && a.Status == 0);
            if (appt is null)
            {
                StatusMessage = "No scheduled appointment for this patient.";
                return;
            }

            await _session.PostWithoutContentAsync($"/api/appointments/{appt.Id}/checkin", ct);
            StatusMessage = "Checked in.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Check-in failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadLabQueueAsync(CancellationToken ct)
    {
        try
        {
            var json = await _session.GetJsonAsync("/api/lab-orders", ct);
            var rows = _session.Deserialize<List<LabRow>>(json) ?? [];
            LabOrders.Clear();
            foreach (var r in rows)
                LabOrders.Add(r);
            StatusMessage = $"Lab orders: {LabOrders.Count}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Lab list failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task DownloadDocumentAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(DocumentId, out var id))
        {
            StatusMessage = "Enter a document GUID.";
            return;
        }

        var dlg = new SaveFileDialog
        {
            FileName = "document.pdf",
            Filter = "PDF|*.pdf|All|*.*",
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _session.SaveBinaryToFileAsync($"/api/documents/{id}", dlg.FileName, ct);
            StatusMessage = "Saved to " + dlg.FileName;
            MessageBox.Show(
                "File saved. Open it from disk and print from your PDF viewer.",
                "CareHub Desktop",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = "Download failed: " + ex.Message;
        }
    }
}

public record PatientRow(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    string DateOfBirth,
    Guid BranchId);

public record AppointmentRow(Guid Id, Guid PatientId, int Status);

public record LabRow(Guid Id, int Status, Guid PatientId);

public record DoctorRow(
    Guid Id,
    string FirstName,
    string LastName,
    string Specialty,
    Guid BranchId,
    bool IsActive);

public record SlotRow(string? SlotTime);

using System.Collections.ObjectModel;
using System.Windows;
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

    [ObservableProperty] private string _bookPatientId = "";
    [ObservableProperty] private string _bookDoctorId = "";
    [ObservableProperty] private string _bookBranchId = "00000000-0000-0000-0000-000000000001";
    /// <summary>Local date/time; parsed with DateTime.TryParse to UTC for the API.</summary>
    [ObservableProperty] private string _bookScheduledAtText =
        DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:mm");
    [ObservableProperty] private int _bookDuration = 30;

    [ObservableProperty] private string _documentId = "";
    [ObservableProperty] private bool _showLabTab;
    public ObservableCollection<LabRow> LabOrders { get; } = new();

    public MainViewModel()
    {
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
            StatusMessage = created is null ? "Patient created." : $"Patient {created.Id} created.";
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
            if (!Guid.TryParse(BookPatientId, out var pid) || !Guid.TryParse(BookDoctorId, out var did)
                || !Guid.TryParse(BookBranchId, out var bid))
            {
                StatusMessage = "Patient, doctor, and branch must be valid GUIDs.";
                return;
            }

            if (!DateTime.TryParse(BookScheduledAtText, out var localWhen))
            {
                StatusMessage = "Could not parse scheduled date/time.";
                return;
            }

            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                patientId = pid,
                doctorId = did,
                branchId = bid,
                scheduledAt = localWhen.ToUniversalTime().ToString("o"),
                durationMinutes = BookDuration,
            });
            await _session.PostJsonAsync("/api/appointments", body, ct);
            StatusMessage = "Appointment booked.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Booking failed: " + ex.Message;
        }
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

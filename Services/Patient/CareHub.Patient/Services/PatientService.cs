using CareHub.Patient.Data;
using CareHub.Patient.Events;
using CareHub.Patient.Exceptions;
using CareHub.Patient.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Patient.Services;

public class PatientService
{
    private readonly PatientDbContext _db;
    private readonly PatientEventPublisher _events;

    public PatientService(PatientDbContext db, PatientEventPublisher events)
    {
        _db = db;
        _events = events;
    }

    public async Task<List<PatientResponse>> SearchAsync(
        string? q, Guid? branchId, bool global, Guid callerBranchId)
    {
        var query = _db.Patients.AsQueryable();

        // Branch scoping
        if (!global && branchId == null)
            query = query.Where(p => p.BranchId == callerBranchId);
        else if (branchId.HasValue)
            query = query.Where(p => p.BranchId == branchId.Value);
        // global=true: no branch filter

        // Partial text search
        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(lower) ||
                p.LastName.ToLower().Contains(lower) ||
                p.PhoneNumber.Contains(q));
        }

        return await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => PatientResponse.FromEntity(p))
            .ToListAsync();
    }

    public async Task<PatientResponse?> GetByIdAsync(Guid id)
    {
        var patient = await _db.Patients.FindAsync(id);
        return patient is null ? null : PatientResponse.FromEntity(patient);
    }

    public async Task<PatientResponse> CreateAsync(
        CreatePatientRequest request, Guid createdByUserId, Guid branchId)
    {
        if (await _db.Patients.AnyAsync(p => p.PhoneNumber == request.PhoneNumber))
            throw new DuplicatePhoneNumberException(request.PhoneNumber);

        var patient = new Models.Patient
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        await _events.PublishPatientCreatedAsync(patient, createdByUserId);

        return PatientResponse.FromEntity(patient);
    }

    public async Task<PatientResponse> UpdateAsync(
        Guid id, UpdatePatientRequest request, Guid updatedByUserId)
    {
        var patient = await _db.Patients.FindAsync(id)
            ?? throw new KeyNotFoundException($"Patient {id} not found.");

        // Phone uniqueness check — exclude self
        if (patient.PhoneNumber != request.PhoneNumber &&
            await _db.Patients.AnyAsync(p => p.PhoneNumber == request.PhoneNumber))
            throw new DuplicatePhoneNumberException(request.PhoneNumber);

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.PhoneNumber = request.PhoneNumber;
        patient.Email = request.Email;
        patient.DateOfBirth = request.DateOfBirth;
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _events.PublishPatientUpdatedAsync(patient, updatedByUserId);

        return PatientResponse.FromEntity(patient);
    }

    public Task<List<object>> GetHistoryAsync(Guid id)
        // Stub: returns empty list until Phase 5 (Appointment Service) populates this
        => Task.FromResult(new List<object>());
}

using CareHub.Schedule.Data;
using CareHub.Schedule.Exceptions;
using CareHub.Schedule.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Schedule.Services;

public class ScheduleService
{
    private readonly ScheduleDbContext _db;

    public ScheduleService(ScheduleDbContext db)
    {
        _db = db;
    }

    public async Task<List<DoctorResponse>> GetDoctorsAsync(string? specialty, Guid? branchId)
    {
        var query = _db.Doctors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(specialty))
            query = query.Where(d => d.Specialty == specialty);

        if (branchId.HasValue)
            query = query.Where(d => d.BranchId == branchId.Value);

        return await query
            .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
            .Select(d => DoctorResponse.FromEntity(d))
            .ToListAsync();
    }

    public async Task<DoctorResponse?> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        return doctor is null ? null : DoctorResponse.FromEntity(doctor);
    }

    public async Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request)
    {
        var doctor = new Doctor
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Specialty = request.Specialty,
            BranchId = request.BranchId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();

        return DoctorResponse.FromEntity(doctor);
    }

    public async Task<ShiftResponse> CreateShiftAsync(Guid doctorId, CreateShiftRequest request)
    {
        if (request.StartTime >= request.EndTime)
            throw new InvalidShiftException("Shift StartTime must be before EndTime.");

        if (request.SlotDurationMinutes <= 0)
            throw new InvalidShiftException("SlotDurationMinutes must be greater than zero.");

        _ = await _db.Doctors.FindAsync(doctorId) ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SlotDurationMinutes = request.SlotDurationMinutes,
            RoomNumber = request.RoomNumber,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync();

        return ShiftResponse.FromEntity(shift);
    }

    public async Task<ShiftResponse> UpdateShiftAsync(Guid shiftId, UpdateShiftRequest request)
    {
        if (request.StartTime >= request.EndTime)
            throw new InvalidShiftException("Shift StartTime must be before EndTime.");

        if (request.SlotDurationMinutes <= 0)
            throw new InvalidShiftException("SlotDurationMinutes must be greater than zero.");

        var shift = await _db.Shifts.FindAsync(shiftId)
            ?? throw new KeyNotFoundException($"Shift {shiftId} not found.");

        shift.Date = request.Date;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.SlotDurationMinutes = request.SlotDurationMinutes;
        shift.RoomNumber = request.RoomNumber;

        await _db.SaveChangesAsync();

        return ShiftResponse.FromEntity(shift);
    }

    public async Task<List<SlotResponse>> GetAvailableSlotsAsync(Guid doctorId, DateOnly date)
    {
        var shifts = await _db.Shifts
            .Where(s => s.DoctorId == doctorId && s.Date == date)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        var slots = new List<SlotResponse>();
        foreach (var shift in shifts)
        {
            var slotDuration = TimeSpan.FromMinutes(shift.SlotDurationMinutes);
            var current = shift.StartTime;
            while (current.Add(slotDuration) <= shift.EndTime)
            {
                slots.Add(new SlotResponse(current));
                current = current.Add(slotDuration);
            }
        }

        return slots;
    }

    public async Task<ValidateSlotResponse> ValidateSlotAsync(ValidateSlotRequest request)
    {
        var shifts = await _db.Shifts
            .Where(s => s.DoctorId == request.DoctorId && s.Date == request.Date)
            .ToListAsync();

        if (shifts.Count == 0)
            return new ValidateSlotResponse(false, "No shift found for doctor on that date.");

        foreach (var shift in shifts)
        {
            var slotDuration = TimeSpan.FromMinutes(shift.SlotDurationMinutes);
            if (request.SlotTime >= shift.StartTime &&
                request.SlotTime.Add(slotDuration) <= shift.EndTime)
            {
                var minutesFromStart = (int)(request.SlotTime.ToTimeSpan() - shift.StartTime.ToTimeSpan()).TotalMinutes;
                if (minutesFromStart % shift.SlotDurationMinutes == 0)
                    return new ValidateSlotResponse(true);
            }
        }

        return new ValidateSlotResponse(false, "Slot does not fall within any valid shift window.");
    }
}

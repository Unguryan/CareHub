namespace CareHub.Schedule.Models;

// Doctor DTOs
public record CreateDoctorRequest(
    string FirstName,
    string LastName,
    string Specialty,
    Guid BranchId);

public record DoctorResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Specialty,
    Guid BranchId,
    bool IsActive,
    DateTime CreatedAt)
{
    public static DoctorResponse FromEntity(Doctor d) =>
        new(d.Id, d.FirstName, d.LastName, d.Specialty, d.BranchId, d.IsActive, d.CreatedAt);
}

// Shift DTOs
public record CreateShiftRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes = 30,
    string? RoomNumber = null);

public record UpdateShiftRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    string? RoomNumber);

public record ShiftResponse(
    Guid Id,
    Guid DoctorId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    string? RoomNumber,
    DateTime CreatedAt)
{
    public static ShiftResponse FromEntity(Shift s) =>
        new(s.Id, s.DoctorId, s.Date, s.StartTime, s.EndTime, s.SlotDurationMinutes, s.RoomNumber, s.CreatedAt);
}

// Slot DTOs
public record SlotResponse(TimeOnly SlotTime);

public record ValidateSlotRequest(Guid DoctorId, DateOnly Date, TimeOnly SlotTime);

public record ValidateSlotResponse(bool IsValid, string? Reason = null);

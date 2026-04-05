namespace CareHub.Schedule.Models;

public class Shift
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public string? RoomNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

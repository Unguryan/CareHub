using CareHub.Schedule.Data;
using CareHub.Schedule.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Schedule.Seed;

public static class ScheduleSeedData
{
    public static readonly Guid DoctorKovalenko = Guid.Parse("D0000001-0000-0000-0000-000000000001");
    public static readonly Guid DoctorShevchenko = Guid.Parse("D0000001-0000-0000-0000-000000000002");
    private static readonly Guid Branch1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Branch2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ScheduleDbContext>();
        if (await db.Doctors.AnyAsync())
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var kovalenko = new Doctor
        {
            Id = DoctorKovalenko,
            FirstName = "Olena",
            LastName = "Kovalenko",
            Specialty = "Cardiology",
            BranchId = Branch1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var shevchenko = new Doctor
        {
            Id = DoctorShevchenko,
            FirstName = "Mykola",
            LastName = "Shevchenko",
            Specialty = "Neurology",
            BranchId = Branch2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.Doctors.AddRange(kovalenko, shevchenko);

        db.Shifts.AddRange(
            new Shift
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorKovalenko,
                Date = today.AddDays(1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(13, 0),
                SlotDurationMinutes = 30,
                RoomNumber = "101",
                CreatedAt = DateTime.UtcNow,
            },
            new Shift
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorShevchenko,
                Date = today.AddDays(1),
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(18, 0),
                SlotDurationMinutes = 45,
                RoomNumber = "205",
                CreatedAt = DateTime.UtcNow,
            });

        await db.SaveChangesAsync();
    }
}

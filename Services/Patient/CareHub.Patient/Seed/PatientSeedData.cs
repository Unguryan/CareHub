using CareHub.Patient.Data;
using Microsoft.EntityFrameworkCore;
using PatientEntity = CareHub.Patient.Models.Patient;

namespace CareHub.Patient.Seed;

public static class PatientSeedData
{
    // These match DefaultBranchId values used in tests
    private static readonly Guid Branch1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Branch2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<PatientDbContext>();
        if (await db.Patients.AnyAsync()) return;

        db.Patients.AddRange(
            new PatientEntity
            {
                Id = Guid.NewGuid(),
                FirstName = "Ivan",
                LastName = "Petrenko",
                PhoneNumber = "+380501234567",
                DateOfBirth = new DateOnly(1985, 3, 15),
                BranchId = Branch1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new PatientEntity
            {
                Id = Guid.NewGuid(),
                FirstName = "Olena",
                LastName = "Kovalenko",
                PhoneNumber = "+380507654321",
                DateOfBirth = new DateOnly(1990, 7, 22),
                BranchId = Branch1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new PatientEntity
            {
                Id = Guid.NewGuid(),
                FirstName = "Mykola",
                LastName = "Shevchenko",
                PhoneNumber = "+380509876543",
                DateOfBirth = new DateOnly(1978, 11, 5),
                BranchId = Branch2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );

        await db.SaveChangesAsync();
    }
}

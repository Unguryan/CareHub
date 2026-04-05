namespace CareHub.Appointment.Seed;

public static class AppointmentSeedData
{
    public static Task SeedAsync(IServiceProvider services)
    {
        _ = services;
        return Task.CompletedTask;
    }
}

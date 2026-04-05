namespace CareHub.Appointment.Exceptions;

public class AppointmentOverlapException : Exception
{
    public AppointmentOverlapException() : base("Doctor already has an overlapping appointment.") { }
}

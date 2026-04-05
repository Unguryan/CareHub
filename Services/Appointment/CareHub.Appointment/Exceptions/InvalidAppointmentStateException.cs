namespace CareHub.Appointment.Exceptions;

public class InvalidAppointmentStateException : Exception
{
    public InvalidAppointmentStateException(string message) : base(message) { }
}

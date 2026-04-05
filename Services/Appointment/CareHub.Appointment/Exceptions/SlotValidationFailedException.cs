namespace CareHub.Appointment.Exceptions;

public class SlotValidationFailedException : Exception
{
    public SlotValidationFailedException(string message) : base(message) { }
}

namespace CareHub.Schedule.Exceptions;

public class InvalidShiftException : Exception
{
    public InvalidShiftException(string message) : base(message) { }
}

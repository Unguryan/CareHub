namespace CareHub.Laboratory.Exceptions;

public class InvalidLabOrderStateException : Exception
{
    public InvalidLabOrderStateException(string message) : base(message) { }
}

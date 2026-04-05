namespace CareHub.Billing.Exceptions;

public class InvalidInvoiceStateException : Exception
{
    public InvalidInvoiceStateException(string message) : base(message) { }
}

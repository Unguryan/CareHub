namespace CareHub.Patient.Exceptions;

public class DuplicatePhoneNumberException : Exception
{
    public DuplicatePhoneNumberException(string phone)
        : base($"A patient with phone number '{phone}' is already registered.") { }
}

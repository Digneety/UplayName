namespace UbisoftName.Exceptions;

public class InvalidLoginDataException : Exception
{
    public InvalidLoginDataException()
    {
        
    }

    public InvalidLoginDataException(string message) : base(message)
    {
        
    }
}
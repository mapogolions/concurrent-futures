namespace Futures;

public class InvalidFutureStateException : Exception
{
    public InvalidFutureStateException() { }
    public InvalidFutureStateException(string message) : base(message) { }
    public InvalidFutureStateException(string message, Exception innerException) : base(message, innerException) { }
}

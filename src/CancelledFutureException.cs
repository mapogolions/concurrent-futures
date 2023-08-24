namespace Futures;

class CancelledFutureException : Exception
{
    public CancelledFutureException() { }
    public CancelledFutureException(string message) : base(message) { }
    public CancelledFutureException(string message, Exception innerException) : base(message, innerException) { }
}

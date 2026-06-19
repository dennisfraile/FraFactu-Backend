namespace FraFactu.Domain.Exceptions;

/// <summary>
/// Base exception para todas las excepciones de dominio
/// </summary>
public abstract class FacturacionException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    protected FacturacionException(
        string message,
        string errorCode,
        int statusCode = 400,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

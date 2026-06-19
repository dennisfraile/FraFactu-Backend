namespace FraFactu.Domain.Exceptions;

/// <summary>
/// Exception para errores de validación
/// </summary>
public class ValidationException : FacturacionException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base(
            "Uno o más errores de validación ocurrieron.",
            "VALIDATION_ERROR",
            400)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base(error, "VALIDATION_ERROR", 400)
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { error } }
        };
    }
}

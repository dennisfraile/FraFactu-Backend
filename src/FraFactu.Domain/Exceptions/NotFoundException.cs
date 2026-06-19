namespace FraFactu.Domain.Exceptions;

/// <summary>
/// Exception cuando no se encuentra un recurso
/// </summary>
public class NotFoundException : FacturacionException
{
    public NotFoundException(string entityName, object key)
        : base(
            $"{entityName} con ID '{key}' no fue encontrado.",
            "NOT_FOUND",
            404)
    {
    }

    public NotFoundException(string message)
        : base(message, "NOT_FOUND", 404)
    {
    }
}

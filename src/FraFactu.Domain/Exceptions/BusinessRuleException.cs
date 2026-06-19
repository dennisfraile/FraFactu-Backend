namespace FraFactu.Domain.Exceptions;

/// <summary>
/// Exception para violaciones de reglas de negocio
/// </summary>
public class BusinessRuleException : FacturacionException
{
    public BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_VIOLATION")
        : base(message, errorCode, 422)
    {
    }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, "BUSINESS_RULE_VIOLATION", 422, innerException)
    {
    }
}

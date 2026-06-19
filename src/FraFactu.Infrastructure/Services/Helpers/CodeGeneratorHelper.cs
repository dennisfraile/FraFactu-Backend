using System.Text.RegularExpressions;

namespace FraFactu.Infrastructure.Services.Helpers
{
    public static class CodeGeneratorHelper
    {
        public static string GenerateNextCode(string prefix, string? lastCode, int totalCount)
        {
            if (string.IsNullOrEmpty(lastCode))
            {
                // Si no hay último código, iniciamos con 1
                return $"{prefix}-001";
            }

            // Intentamos extraer el número del final del código
            // Busca un guion seguido de dígitos al final de la cadena: "-005"
            var match = Regex.Match(lastCode, $@"^.*{Regex.Escape(prefix)}[-]?(\d+)$", RegexOptions.IgnoreCase);

            if (match.Success && int.TryParse(match.Groups[1].Value, out int lastNumber))
            {
                // Si encontramos un número, incrementamos
                return $"{prefix}-{lastNumber + 1:D3}";
            }

            // Intento alternativo más genérico: buscar cualquier secuencia de dígitos al final
            match = Regex.Match(lastCode, @"(\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out lastNumber))
            {
                // Mantener el prefijo original del lastCode si es diferente, o usar el nuevo? 
                // Por consistencia, si estamos forzando un prefijo, intentamos usarlo.
                return $"{prefix}-{lastNumber + 1:D3}";
            }

            // Si falla el parsing, fallback al conteo total + 1
            // Esto maneja casos donde el código anterior era "PRINCIPAL" (sin números)
            return $"{prefix}-{totalCount + 1:D3}";
        }
    }
}

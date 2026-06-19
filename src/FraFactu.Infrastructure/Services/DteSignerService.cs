using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using FraFactu.Application.Interfaces.Hacienda;

namespace FraFactu.Infrastructure.Services
{
    public class DteSignerService : IDteSignerService
    {
        public string FirmarDocumento(string jsonPayload, string llavePrivada, string passwordPrivada)
        {
            try
            {
                // 1. Limpiar llave de encabezados y espacios si es necesario (simple trim)
                // Dependiendo del formato real (PEM vs Base64 puro), la lógica puede variar.
                // Asumimos formato PEM o Base64 directo.

                // NOTA: Para este ejemplo básico, usaremos RSA estándar.
                // En producción con MH El Salvador, suelen requerir JWS con cabeceras específicas.

                using var rsa = RSA.Create();

                // Importar llave. .NET 8 soporta importar desde PEM encriptado si hay password.
                if (!string.IsNullOrEmpty(passwordPrivada))
                {
                    rsa.ImportFromEncryptedPem(llavePrivada, passwordPrivada);
                }
                else
                {
                    rsa.ImportFromPem(llavePrivada);
                }

                // 2. Crear JWS Header
                var header = new { alg = "RS512", typ = "JWT" };
                var headerJson = JsonSerializer.Serialize(header);
                var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));

                // 3. Payload a Base64Url
                var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(jsonPayload));

                // 4. Data a firmar: header + "." + payload
                var stringToSign = $"{headerBase64}.{payloadBase64}";
                var bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

                // 5. Firmar
                var signatureBytes = rsa.SignData(bytesToSign, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                var signatureBase64 = Base64UrlEncode(signatureBytes);

                // 6. Retornar JWS completo
                return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al firmar DTE: {ex.Message}", ex);
            }
        }

        public string GenerarHash(string jsonPayload)
        {
            using var sha512 = SHA512.Create();
            var bytes = Encoding.UTF8.GetBytes(jsonPayload);
            var hashBytes = sha512.ComputeHash(bytes);

            // Convertir a Hexadecimal (formato común para hashes de integridad)
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Remover padding
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }
    }
}

using System.Security.Cryptography;

namespace FraFactu.Infrastructure.Security
{
    /// <summary>
    /// Generación de secretos para los flujos de identidad (F2): contraseñas
    /// temporales y tokens de reset. Usa un RNG criptográfico.
    /// </summary>
    public static class SecureTokenGenerator
    {
        // Alfabeto sin caracteres ambiguos (0/O, 1/l/I) para contraseñas que un
        // humano debe teclear desde un correo.
        private const string PasswordAlphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        /// <summary>
        /// Genera una contraseña temporal aleatoria criptográficamente segura.
        /// </summary>
        public static string GenerateTemporaryPassword(int length = 12)
        {
            if (length < 8) length = 8;

            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                var idx = RandomNumberGenerator.GetInt32(PasswordAlphabet.Length);
                chars[i] = PasswordAlphabet[idx];
            }
            return new string(chars);
        }

        /// <summary>
        /// Genera el token de reset que viaja en el enlace del correo (en claro).
        /// Se devuelve en base64url para que sea seguro en una URL.
        /// </summary>
        public static string GenerateResetToken(int bytes = 32)
        {
            var buffer = RandomNumberGenerator.GetBytes(bytes);
            return Convert.ToBase64String(buffer)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        /// <summary>
        /// Hash SHA-256 (hex) del token. En BD se guarda este hash, nunca el
        /// token en claro, igual que con una contraseña.
        /// </summary>
        public static string Sha256Hex(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Services
{
    public class AesEncryptionService : IEncryptionService
    {
        private const string EncryptedPrefix = "ENC:";
        private const int NonceSize = 12; // AES-GCM standard nonce size
        private const int TagSize = 16;   // AES-GCM standard tag size
        private readonly byte[] _key;
        private readonly ILogger<AesEncryptionService> _logger;

        public AesEncryptionService(IOptions<EncryptionSettings> settings, ILogger<AesEncryptionService> logger)
        {
            _logger = logger;
            var masterKey = settings.Value.MasterKey;

            if (string.IsNullOrWhiteSpace(masterKey))
            {
                throw new InvalidOperationException(
                    "Encryption:MasterKey no está configurada. " +
                    "Configure la variable en appsettings.json, user-secrets o Azure App Settings.");
            }

            _key = Convert.FromBase64String(masterKey);

            if (_key.Length != 32)
            {
                throw new InvalidOperationException(
                    $"Encryption:MasterKey debe ser exactamente 32 bytes (256 bits) en Base64. Tamaño actual: {_key.Length} bytes.");
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            if (IsEncrypted(plainText))
                return plainText; // Ya está encriptado

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var cipherText = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherText, tag);

            // Format: nonce + ciphertext + tag
            var result = new byte[NonceSize + cipherText.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(cipherText, 0, result, NonceSize, cipherText.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + cipherText.Length, TagSize);

            return EncryptedPrefix + Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            if (!IsEncrypted(cipherText))
                return cipherText; // Dato legacy sin encriptar, retornar tal cual

            try
            {
                var encryptedData = Convert.FromBase64String(cipherText.Substring(EncryptedPrefix.Length));

                var nonce = new byte[NonceSize];
                var tag = new byte[TagSize];
                var cipherBytes = new byte[encryptedData.Length - NonceSize - TagSize];

                Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
                Buffer.BlockCopy(encryptedData, NonceSize, cipherBytes, 0, cipherBytes.Length);
                Buffer.BlockCopy(encryptedData, NonceSize + cipherBytes.Length, tag, 0, TagSize);

                var plainBytes = new byte[cipherBytes.Length];

                using var aes = new AesGcm(_key, TagSize);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desencriptar valor. Posible key incorrecta o dato corrupto.");
                throw new InvalidOperationException("No se pudo desencriptar el valor. Verifique que la MasterKey sea correcta.", ex);
            }
        }

        public bool IsEncrypted(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix);
        }
    }
}

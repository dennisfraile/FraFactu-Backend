namespace FraFactu.Application.Interfaces
{
    public interface IEncryptionService
    {
        /// <summary>
        /// Encripta un texto plano. Retorna string con prefijo "ENC:" + Base64(nonce|ciphertext|tag)
        /// </summary>
        string Encrypt(string plainText);

        /// <summary>
        /// Desencripta un texto previamente encriptado con Encrypt().
        /// Si el valor no tiene prefijo "ENC:", lo retorna tal cual (compatibilidad con datos legacy).
        /// </summary>
        string Decrypt(string cipherText);

        /// <summary>
        /// Indica si el valor ya está encriptado (tiene prefijo "ENC:")
        /// </summary>
        bool IsEncrypted(string value);
    }
}

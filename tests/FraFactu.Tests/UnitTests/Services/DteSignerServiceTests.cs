using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FraFactu.Infrastructure.Services;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services
{
    public class DteSignerServiceTests
    {
        private readonly DteSignerService _signerService;

        public DteSignerServiceTests()
        {
            _signerService = new DteSignerService();
        }

        // ── Helpers Base64Url (espejan lo que hace el firmador / lo que MH usa al verificar) ──
        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }

        private static string Base64UrlEncode(byte[] input) =>
            Convert.ToBase64String(input).Split('=')[0].Replace('+', '-').Replace('/', '_');

        [Fact]
        public void FirmarDocumento_ShouldReturnValidJws_WhenKeyIsStandardRSA()
        {
            // Arrange
            // Generar una llave RSA temporal para prueba
            using var rsa = RSA.Create(2048);
            var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
            var payload = "{\"test\": \"data\"}";

            // Act
            var jws = _signerService.FirmarDocumento(payload, privateKeyPem, "");

            // Assert
            Assert.NotNull(jws);
            var parts = jws.Split('.');
            Assert.Equal(3, parts.Length); // Header.Payload.Signature

            // Verificar Header
            // RS512 header {"alg":"RS512","typ":"JWT"} en Base64Url
            // eyJhbGciOiJSUzUxMiIsInR5cCI6IkpXVCJ9 ==> {"alg":"RS512","typ":"JWT"}
            Assert.StartsWith("eyJhbGciOiJSUzUxMiIsInR5cCI6IkpXVCJ9", parts[0]);
        }

        [Fact]
        public void FirmarDocumento_FirmaDebeSerVerificableConLlavePublica()
        {
            // Round-trip: así es como MH valida la firma (con la llave pública del emisor).
            using var rsa = RSA.Create(2048);
            var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
            var payload = "{\"identificacion\":{\"tipoDte\":\"01\"},\"resumen\":{\"totalPagar\":113.00}}";

            var jws = _signerService.FirmarDocumento(payload, privateKeyPem, "");
            var parts = jws.Split('.');

            var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
            var signature = Base64UrlDecode(parts[2]);

            using var publicKey = RSA.Create();
            publicKey.ImportFromPem(rsa.ExportRSAPublicKeyPem());
            var firmaValida = publicKey.VerifyData(
                signingInput, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            Assert.True(firmaValida);
        }

        [Fact]
        public void FirmarDocumento_PayloadDebeDecodificarAlJsonOriginalSinAlterar()
        {
            // El DTE va como JSON crudo en Base64Url: no debe escaparse ni alterarse (acentos, ñ, símbolos).
            using var rsa = RSA.Create(2048);
            var payload = "{\"emisor\":{\"nombre\":\"COMPAÑÍA DE PRUEBA S.A. de C.V.\"},\"obs\":\"café & té\"}";

            var jws = _signerService.FirmarDocumento(payload, rsa.ExportRSAPrivateKeyPem(), "");
            var payloadDecodificado = Encoding.UTF8.GetString(Base64UrlDecode(jws.Split('.')[1]));

            Assert.Equal(payload, payloadDecodificado);
        }

        [Fact]
        public void FirmarDocumento_PartesDebenSerBase64UrlSinPadding()
        {
            using var rsa = RSA.Create(2048);
            var jws = _signerService.FirmarDocumento("{\"a\":1}", rsa.ExportRSAPrivateKeyPem(), "");

            foreach (var parte in jws.Split('.'))
            {
                Assert.DoesNotContain('=', parte);
                Assert.DoesNotContain('+', parte);
                Assert.DoesNotContain('/', parte);
            }
        }

        [Fact]
        public void FirmarDocumento_DebeFuncionarConLlaveEncriptadaConPassword()
        {
            // Caso real de MH El Salvador: la llave privada viene encriptada con contraseña.
            using var rsa = RSA.Create(2048);
            const string password = "claveDePrueba123";
            var pbe = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000);
            var encryptedPem = rsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbe);
            var payload = "{\"test\":\"contingencia\"}";

            var jws = _signerService.FirmarDocumento(payload, encryptedPem, password);
            var parts = jws.Split('.');

            using var publicKey = RSA.Create();
            publicKey.ImportFromPem(rsa.ExportRSAPublicKeyPem());
            var firmaValida = publicKey.VerifyData(
                Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"),
                Base64UrlDecode(parts[2]), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            Assert.True(firmaValida);
        }

        [Fact]
        public void FirmarDocumento_FirmaNoDebeValidarSiElPayloadFueAlterado()
        {
            // Garantía de integridad: si alguien altera el contenido, la firma deja de validar.
            using var rsa = RSA.Create(2048);
            var jws = _signerService.FirmarDocumento("{\"monto\":100}", rsa.ExportRSAPrivateKeyPem(), "");
            var parts = jws.Split('.');

            var payloadAlterado = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"monto\":999}"));
            var signingInputAlterado = Encoding.UTF8.GetBytes($"{parts[0]}.{payloadAlterado}");

            using var publicKey = RSA.Create();
            publicKey.ImportFromPem(rsa.ExportRSAPublicKeyPem());
            var firmaValida = publicKey.VerifyData(
                signingInputAlterado, Base64UrlDecode(parts[2]),
                HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            Assert.False(firmaValida);
        }

        [Fact]
        public void GenerarHash_ShouldReturnSha512Hex()
        {
            // Arrange
            var payload = "test data";

            // Act
            var hash = _signerService.GenerarHash(payload);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(128, hash.Length); // SHA-512 hex string length (512 bits / 4 bits per hex char = 128 chars)
        }
    }
}

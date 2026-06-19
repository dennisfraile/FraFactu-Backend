namespace FraFactu.Infrastructure.Helpers
{
    /// <summary>
    /// Ayudante para validaciones de identificación según estándar de Hacienda
    /// </summary>
    public static class IdentificacionHelper
    {
        /// <summary>
        /// Valida el formato del NIT (9 o 14 dígitos)
        /// </summary>
        public static bool ValidarNIT(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return false;

            // Remover guiones si existen
            var nitLimpio = nit.Replace("-", "");

            // NIT debe tener 9 o 14 dígitos
            return (nitLimpio.Length == 9 || nitLimpio.Length == 14) &&
                   nitLimpio.All(c => char.IsDigit(c));
        }

        /// <summary>
        /// Valida el formato del NRC (2-8 dígitos)
        /// </summary>
        public static bool ValidarNRC(string nrc)
        {
            if (string.IsNullOrWhiteSpace(nrc))
                return false;

            // Remover guiones si existen
            var nrcLimpio = nrc.Replace("-", "");

            return nrcLimpio.Length >= 2 && nrcLimpio.Length <= 8 &&
                   nrcLimpio.All(c => char.IsDigit(c));
        }

        /// <summary>
        /// Valida el formato del DUI (XXXXXXXX-X)
        /// </summary>
        public static bool ValidarDUI(string dui)
        {
            if (string.IsNullOrWhiteSpace(dui))
                return false;

            // Con guión: XXXXXXXX-X (8 dígitos, guión, 1 dígito)
            if (dui.Length == 10 && dui[8] == '-')
            {
                var partes = dui.Split('-');
                return partes.Length == 2 &&
                       partes[0].Length == 8 && partes[0].All(char.IsDigit) &&
                       partes[1].Length == 1 && partes[1].All(char.IsDigit);
            }

            // Sin guión: XXXXXXXXX (9 dígitos)
            return dui.Length == 9 && dui.All(char.IsDigit);
        }

        /// <summary>
        /// Obtiene el tipo de documento basado en el código
        /// </summary>
        public static string ObtenerNombreTipoDocumento(string codigo)
        {
            return codigo switch
            {
                "36" => "NIT",
                "13" => "DUI",
                "02" => "Carnet de Residente",
                "03" => "Pasaporte",
                "37" => "Otro",
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Valida el código de generación (GUID formato UUID)
        /// </summary>
        public static bool ValidarCodigoGeneracion(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 36)
                return false;

            return Guid.TryParse(codigo, out _);
        }

        /// <summary>
        /// Genera un código de generación válido (GUID en mayúsculas)
        /// </summary>
        public static string GenerarCodigoGeneracion()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        /// <summary>
        /// Valida el formato del Número de Control de Hacienda
        /// Formato: DTE-01-XXXXXXXX-000000000000000
        /// </summary>
        public static bool ValidarNumeroControl(string numeroControl)
        {
            if (string.IsNullOrWhiteSpace(numeroControl))
                return false;

            // Debe tener exactamente 31 caracteres
            if (numeroControl.Length != 31)
                return false;

            // Validar formato con regex
            var partes = numeroControl.Split('-');

            if (partes.Length != 4)
                return false;

            return partes[0] == "DTE" &&
                   partes[1] == "01" &&
                   partes[2].Length == 8 && partes[2].All(c => char.IsLetterOrDigit(c)) &&
                   partes[3].Length == 15 && partes[3].All(char.IsDigit);
        }

        /// <summary>
        /// Genera un Número de Control según formato de Hacienda
        /// </summary>
        /// <param name="nit">NIT del emisor</param>
        /// <param name="correlativo">Número correlativo</param>
        /// <returns>Número de Control en formato DTE-01-XXXXXXXX-000000000000000</returns>
        public static string GenerarNumeroControl(string nit, int correlativo)
        {
            // Limpiar el NIT de guiones y tomar primeros 8 caracteres
            var nitLimpio = nit.Replace("-", "");
            var nitParte = nitLimpio.Length >= 8
                ? nitLimpio.Substring(0, 8)
                : nitLimpio.PadRight(8, '0');

            // Correlativo con 15 dígitos
            var correlativoParte = correlativo.ToString("D15");

            return $"DTE-01-{nitParte}-{correlativoParte}";
        }

        /// <summary>
        /// Valida formato de correo electrónico
        /// </summary>
        public static bool ValidarCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(correo);
                return addr.Address == correo;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida formato de teléfono de El Salvador
        /// </summary>
        public static bool ValidarTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return false;

            // Remover guiones y espacios
            var telefonoLimpio = telefono.Replace("-", "").Replace(" ", "");

            // Debe tener 8 dígitos (formato SV) o hasta 15 con código de país
            return (telefonoLimpio.Length >= 8 && telefonoLimpio.Length <= 15) &&
                   telefonoLimpio.All(char.IsDigit);
        }

        /// <summary>
        /// Formatea un NIT para visualización (con guiones)
        /// </summary>
        public static string FormatearNIT(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return string.Empty;

            var nitLimpio = nit.Replace("-", "");

            if (nitLimpio.Length == 14)
            {
                // Formato: 0000-000000-000-0
                return $"{nitLimpio.Substring(0, 4)}-{nitLimpio.Substring(4, 6)}-{nitLimpio.Substring(10, 3)}-{nitLimpio.Substring(13, 1)}";
            }
            else if (nitLimpio.Length == 9)
            {
                // Formato corto: 000000000
                return nitLimpio;
            }

            return nit;
        }

        /// <summary>
        /// Formatea un DUI para visualización (con guión)
        /// </summary>
        public static string FormatearDUI(string dui)
        {
            if (string.IsNullOrWhiteSpace(dui))
                return string.Empty;

            var duiLimpio = dui.Replace("-", "");

            if (duiLimpio.Length == 9)
            {
                // Formato: XXXXXXXX-X
                return $"{duiLimpio.Substring(0, 8)}-{duiLimpio.Substring(8, 1)}";
            }

            return dui;
        }
    }
}

using System.Linq;

namespace FraFactu.Infrastructure.Helpers
{
    /// <summary>
    /// Construye el Número de Control de los DTE según la Normativa V2.0 del MH.
    ///
    /// Formato: <c>DTE-{tipoDte}-(M|B|S|P)###P###-{correlativo:D15}</c> (31 caracteres).
    /// El bloque central es: letra de tipo de establecimiento + 3 dígitos de código de
    /// establecimiento + 'P' + 3 dígitos de punto de venta.
    /// </summary>
    public static class NumeroControlHelper
    {
        public static string Generar(
            string tipoDte,
            string? tipoEstablecimientoCodigo,
            string? codEstable,
            string? codPuntoVenta,
            int correlativo)
        {
            var letra = LetraTipoEstablecimiento(tipoEstablecimientoCodigo);
            var establecimiento = Normalizar3(codEstable);
            var puntoVenta = Normalizar3(codPuntoVenta);
            var correlativoParte = correlativo.ToString("D15");

            return $"DTE-{tipoDte}-{letra}{establecimiento}P{puntoVenta}-{correlativoParte}";
        }

        /// <summary>
        /// Mapea el código CAT-009 (seed del sistema) a la letra del numeroControl:
        /// 01=Casa Matriz→M, 02=Sucursal→S, 04=Bodega→B, 07=Patio→P.
        /// Tipos atípicos (20 Oficina Administrativa, 99 Otros) o desconocidos → M.
        /// </summary>
        public static char LetraTipoEstablecimiento(string? codigo) => codigo switch
        {
            "01" => 'M',
            "02" => 'S',
            "04" => 'B',
            "07" => 'P',
            _ => 'M'
        };

        /// <summary>
        /// Normaliza un código a exactamente 3 dígitos: descarta lo no numérico
        /// (p. ej. el prefijo 'P'), rellena con ceros a la izquierda y toma los
        /// últimos 3. Vacío o nulo → "001".
        /// </summary>
        public static string Normalizar3(string? codigo)
        {
            var digitos = new string((codigo ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digitos.Length == 0)
                digitos = "1";
            digitos = digitos.PadLeft(3, '0');
            return digitos.Substring(digitos.Length - 3);
        }
    }
}

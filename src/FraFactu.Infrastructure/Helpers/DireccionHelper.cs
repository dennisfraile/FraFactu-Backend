namespace FraFactu.Infrastructure.Helpers
{
    /// <summary>
    /// Ayudante para operaciones con direcciones según est ándar de Hacienda
    /// </summary>
    public static class DireccionHelper
    {
        private static readonly Dictionary<string, string> NombresDepartamentos = new()
        {
            { "01", "Ahuachapán" },
            { "02", "Santa Ana" },
            { "03", "Sonsonate" },
            { "04", "Chalatenango" },
            { "05", "La Libertad" },
            { "06", "San Salvador" },
            { "07", "Cuscatlán" },
            { "08", "La Paz" },
            { "09", "Cabañas" },
            { "10", "San Vicente" },
            { "11", "Usulután" },
            { "12", "San Miguel" },
            { "13", "Morazán" },
            { "14", "La Unión" }
        };

        /// <summary>
        /// Obtiene el nombre del departamento
        /// </summary>
        public static string ObtenerNombreDepartamento(string codigoDepartamento)
        {
            return NombresDepartamentos.TryGetValue(codigoDepartamento,
                out var nombre) ? nombre : "Desconocido";
        }

        /// <summary>
        /// Formatea una dirección como texto
        /// </summary>
        public static string FormatearDireccion(string departamento, string municipio, string complemento)
        {
            return $"{ObtenerNombreDepartamento(departamento)}, " +
                   $"Municipio {municipio}, {complemento}";
        }

        /// <summary>
        /// Valida que el departamento sea válido (01-14)
        /// </summary>
        public static bool ValidarDepartamento(string departamento)
        {
            if (string.IsNullOrWhiteSpace(departamento))
                return false;

            return NombresDepartamentos.ContainsKey(departamento);
        }

        /// <summary>
        /// Valida que el municipio sea válido para el departamento
        /// </summary>
        public static bool ValidarMunicipioPorDepartamento(string departamento, string municipio)
        {
            if (!int.TryParse(municipio, out var mun) || !int.TryParse(departamento, out var dep))
                return false;

            // Validaciones específicas por departamento según cantidad de municipios
            return dep switch
            {
                1 => mun >= 1 && mun <= 19,  // San Salvador - 19 municipios
                2 => mun >= 1 && mun <= 22,  // La Libertad - 22 municipios
                3 => mun >= 1 && mun <= 33,  // Chalatenango - 33 municipios
                4 => mun >= 1 && mun <= 16,  // Cuscatlán - 16 municipios
                5 => mun >= 1 && mun <= 22,  // La Paz - 22 municipios
                6 => mun >= 1 && mun <= 9,   // Cabañas - 9 municipios
                7 => mun >= 1 && mun <= 13,  // San Vicente - 13 municipios
                8 => mun >= 1 && mun <= 18,  // La Unión - 18 municipios
                9 => mun >= 1 && mun <= 26,  // Morazán - 26 municipios
                10 => mun >= 1 && mun <= 20, // San Miguel - 20 municipios
                11 => mun >= 1 && mun <= 23, // Usulután - 23 municipios
                12 => mun >= 1 && mun <= 13, // Santa Ana - 13 municipios
                13 => mun >= 1 && mun <= 16, // Sonsonate - 16 municipios
                14 => mun >= 1 && mun <= 12, // Ahuachapán - 12 municipios
                _ => false
            };
        }

        /// <summary>
        /// Valida que el complemento de dirección tenga longitud adecuada
        /// </summary>
        public static bool ValidarComplemento(string complemento)
        {
            if (string.IsNullOrWhiteSpace(complemento))
                return false;

            return complemento.Length >= 5 && complemento.Length <= 200;
        }

        /// <summary>
        /// Obtiene todos los códigos de departamentos válidos
        /// </summary>
        public static List<string> ObtenerCodigosDepartamentos()
        {
            return NombresDepartamentos.Keys.ToList();
        }

        /// <summary>
        /// Obtiene el mapeo completo de departamentos
        /// </summary>
        public static Dictionary<string, string> ObtenerDepartamentos()
        {
            return new Dictionary<string, string>(NombresDepartamentos);
        }
    }
}

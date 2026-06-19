using Xunit;
using FluentValidation.TestHelper;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Validators.Facturas;

namespace FraFactu.Tests.Validators
{
    /// <summary>
    /// Tests para validaciones de Facturas Electrónicas según estándar Hacienda
    /// </summary>
    public class FacturaValidatorsTests
    {
        private readonly CreateFacturaElectronicaDtoValidator _validator;

        public FacturaValidatorsTests()
        {
            _validator = new CreateFacturaElectronicaDtoValidator();
        }

        [Fact]
        public void CreateFactura_ConDatosValidos_DeberiaValidarCorrectamente()
        {
            // Arrange
            var dto = CrearFacturaValida();

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void CreateFactura_SinSucursal_DeberiaFallar()
        {
            // Arrange
            var dto = CrearFacturaValida();
            dto.SucursalId = 0;

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SucursalId);
        }

        [Fact]
        public void CreateFactura_SinIdentificacion_DeberiaFallar()
        {
            // Arrange
            var dto = CrearFacturaValida();
            dto.Identificacion = null!;

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identificacion);
        }

        // Test removido: Receptor es opcional, no genera error de validación cuando es null
        // El validador solo valida cuando Receptor != null (línea 34-38 del validator)

        [Fact]
        public void CreateFactura_SinCuerpoDocumento_DeberiaFallar()
        {
            // Arrange
            var dto = CrearFacturaValida();
            dto.CuerpoDocumento = new List<ItemDocumentoDto>();

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CuerpoDocumento);
        }

        [Fact]
        public void CreateFactura_SinResumen_DeberiaFallar()
        {
            // Arrange
            var dto = CrearFacturaValida();
            dto.Resumen = null!;

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Resumen);
        }

        // Tests de Ambiente COMENTADOS: El ambiente ya no se envía en IdentificacionDto
        // Se obtiene automáticamente del emisor asociado
        /*
        [Theory]
        [InlineData("00")] // Prueba
        [InlineData("01")] // Producción
        public void Identificacion_ConAmbienteValido_DeberiaValidar(string ambiente)
        {
            // Arrange
            var validator = new IdentificacionDtoValidator();
            var dto = new IdentificacionDto
            {
                Ambiente = ambiente,
                TipoDte = "01",
                TipoModelo = 1,
                TipoOperacion = 1
            };

            // Act
            var result = validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Ambiente);
        }

        [Fact]
        public void Identificacion_ConAmbienteInvalido_DeberiaFallar()
        {
            // Arrange
            var validator = new IdentificacionDtoValidator();
            var dto = new IdentificacionDto
            {
                Ambiente = "99", // Inválido
                TipoDte = "01",
                TipoModelo = 1,
                TipoOperacion = 1
            };

            // Act
            var result = validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Ambiente);
        }
        */

        [Fact]
        public void ItemDocumento_ConIVA13Porciento_DeberiaCalcularCorrectamente()
        {
            // Arrange
            var validator = new ItemDocumentoDtoValidator();
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Codigo = "PROD001",
                Descripcion = "Producto de prueba",
                PrecioUni = 100.00m,
                MontoDescuento = 0,
                VentaNoSuj = 0,
                VentaExenta = 0,
                VentaGravada = 1000.00m,
                Tributos = new List<string> { "20" },
                IvaItem = 130.00m // 13% de 1000
            };

            // Act
            var result = validator.TestValidate(item);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        // Test removido: ItemDocumentoDtoValidator NO valida corrección del IVA
        // Solo valida que IvaItem >= 0. El cálculo se valida en el servicio

        [Fact]
        public void Resumen_ConCuadreCorrectoDeItems_DeberiaValidar()
        {
            // Arrange
            var validator = new ResumenDtoValidator();
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0,
                TotalExenta = 0,
                TotalGravada = 1000.00m,
                TotalDescu = 0,
                SubTotal = 1000.00m,
                TotalIva = 130.00m,
                IvaRete1 = 0,
                ReteRenta = 0,
                MontoTotalOperacion = 1130.00m,
                TotalPagar = 1130.00m,
                TotalLetras = "MIL CIENTO TREINTA DÓLARES CON 00/100",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 1130.00m }
                }
            };

            // Act
            var result = validator.TestValidate(resumen);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Resumen_ConSumaPagosIncorrecta_DeberiaFallar()
        {
            // Arrange
            var validator = new ResumenDtoValidator();
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0,
                TotalExenta = 0,
                TotalGravada = 1000.00m,
                TotalDescu = 0,
                SubTotal = 1000.00m,
                TotalIva = 130.00m,
                IvaRete1 = 0,
                ReteRenta = 0,
                MontoTotalOperacion = 1130.00m,
                TotalPagar = 1130.00m,
                TotalLetras = "MIL CIENTO TREINTA DÓLARES CON 00/100",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 1000.00m } // Incorrecto
                }
            };

            // Act
            var result = validator.TestValidate(resumen);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Pagos);
        }

        // ═══ Versión del DTE según Normativa V2.0 ═══

        [Theory]
        [InlineData("01", 2)] // FE → fe-f-v2.json
        [InlineData("03", 4)] // CCF → fe-ccf-v4.json
        public void Identificacion_ConVersionV2Correcta_DeberiaValidar(string tipoDte, int version)
        {
            var validator = new IdentificacionDtoValidator();
            var dto = new IdentificacionDto { TipoDte = tipoDte, Version = version };

            var result = validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Version);
        }

        [Theory]
        [InlineData("01", 1)] // versión vieja de FE ya no es válida
        [InlineData("03", 3)] // versión vieja de CCF ya no es válida
        public void Identificacion_ConVersionVieja_DeberiaFallar(string tipoDte, int version)
        {
            var validator = new IdentificacionDtoValidator();
            var dto = new IdentificacionDto { TipoDte = tipoDte, Version = version };

            var result = validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Version);
        }

        #region Helpers

        private CreateFacturaElectronicaDto CrearFacturaValida()
        {
            return new CreateFacturaElectronicaDto
            {
                SucursalId = 1,
                Identificacion = new IdentificacionDto
                {
                    // Ambiente se obtiene del emisor, no se envía en el DTO
                    TipoDte = "01",
                    Version = 2, // Normativa V2.0: FE(01) → versión 2
                    TipoModelo = 1,
                    TipoOperacion = 1,
                    NumeroControl = "DTE-01-M001P001-000000000000001",
                    CodigoGeneracion = "12345678-1234-1234-1234-123456789012",
                    FechaEmision = DateTime.Today,
                    HoraEmision = "12:00:00"
                },
                Receptor = new ReceptorDteDto
                {
                    TipoDocumento = "36",
                    NumDocumento = "06142812991015",
                    Nombre = "Cliente de Prueba",
                    CodActividad = "01111",
                    DescActividad = "Actividad económica",
                    Direccion = new DireccionDto
                    {
                        Departamento = "06",
                        Municipio = "14",
                        Distrito = "13",
                        Complemento = "Calle Principal #123"
                    },
                    Telefono = "2222-2222",
                    Correo = "cliente@example.com"
                },
                CuerpoDocumento = new List<ItemDocumentoDto>
                {
                    new ItemDocumentoDto
                    {
                        NumItem = 1,
                        TipoItem = 1,
                        Cantidad = 1,
                        UniMedida = 59,
                        Codigo = "PROD001",
                        Descripcion = "Producto de prueba",
                        PrecioUni = 1000.00m,
                        MontoDescuento = 0,
                        VentaNoSuj = 0,
                        VentaExenta = 0,
                        VentaGravada = 1000.00m,
                        Tributos = new List<string> { "20" },
                        IvaItem = 115.04m // Factura (01): (1000 / 1.13) × 0.13 = 115.04
                    }
                },
                Resumen = new ResumenDto
                {
                    TotalNoSuj = 0,
                    TotalExenta = 0,
                    TotalGravada = 1000.00m,
                    TotalDescu = 0,
                    SubTotal = 1000.00m,
                    TotalIva = 115.04m,
                    IvaRete1 = 0,
                    ReteRenta = 0,
                    MontoTotalOperacion = 1000.00m, // Factura: SubTotal + Tributos = 1000 + 0
                    TotalPagar = 1000.00m,
                    TotalLetras = "MIL DÓLARES CON 00/100",
                    CondicionOperacion = 1,
                    Pagos = new List<PagoDto>
                    {
                        new PagoDto { CatFormaPagoId = 1, Monto = 1000.00m }
                    }
                }
            };
        }

        #endregion
    }
}

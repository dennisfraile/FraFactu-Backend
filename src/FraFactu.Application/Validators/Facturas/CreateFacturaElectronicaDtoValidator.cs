using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador principal para Factura Electrónica según estándar de Hacienda
    /// </summary>
    public class CreateFacturaElectronicaDtoValidator : AbstractValidator<DTOs.Facturas.CreateFacturaElectronicaDto>
    {
        public CreateFacturaElectronicaDtoValidator()
        {
            RuleFor(x => x.Identificacion)
                .NotNull()
                .SetValidator(new IdentificacionDtoValidator())
                .WithMessage("Identificación requerida y debe ser válida");

            RuleFor(x => x.CuerpoDocumento)
                .NotEmpty()
                .WithMessage("La factura debe tener al menos un ítem");

            RuleFor(x => x.CuerpoDocumento)
                .Must(x => x.Count <= 2000)
                .When(x => x.CuerpoDocumento != null)
                .WithMessage("La factura no puede tener más de 2000 ítems");

            RuleForEach(x => x.CuerpoDocumento)
                .SetValidator(new ItemDocumentoDtoValidator());

            RuleFor(x => x.Resumen)
                .NotNull()
                .SetValidator(new ResumenDtoValidator())
                .WithMessage("Resumen requerido y debe ser válido");

            When(x => x.Receptor != null, () =>
            {
                RuleFor(x => x.Receptor!)
                    .SetValidator(new ReceptorDteDtoValidator());
            });

            RuleFor(x => x.SucursalId)
                .GreaterThan(0)
                .When(x => x.Identificacion?.TipoDte != "05" && x.Identificacion?.TipoDte != "06")
                .WithMessage("Debe especificar una sucursal válida");

            RuleFor(x => x.VendedorId)
                .GreaterThan(0)
                .When(x => x.VendedorId.HasValue)
                .WithMessage("El VendedorId debe ser mayor que 0 si se especifica");

            RuleFor(x => x.CajaId)
                .GreaterThan(0)
                .When(x => x.CajaId.HasValue)
                .WithMessage("El CajaId debe ser mayor que 0 si se especifica");

            // ===================================================================
            // VALIDACIONES NOTA DE CRÉDITO (DTE-05)
            // ===================================================================
            When(x => x.Identificacion != null && x.Identificacion.TipoDte == "05", () =>
            {
                // NC requiere al menos 1 documento relacionado (máximo 50)
                RuleFor(x => x.DocumentosRelacionados)
                    .NotNull().WithMessage("Nota de Crédito requiere al menos un documento relacionado")
                    .Must(dr => dr != null && dr.Count >= 1)
                    .WithMessage("Nota de Crédito requiere al menos un documento relacionado")
                    .Must(dr => dr == null || dr.Count <= 50)
                    .WithMessage("No puede tener más de 50 documentos relacionados");

                // Validar cada documento relacionado
                RuleForEach(x => x.DocumentosRelacionados)
                    .SetValidator(new DocumentoRelacionadoDtoValidator());

                // Según esquema MH fe-nc-v3.json, solo permite tipoDocumento "03" (CCF) y "07" (CR)
                RuleForEach(x => x.DocumentosRelacionados)
                    .Must(dr => dr.TipoDocumento == "03" || dr.TipoDocumento == "07")
                    .WithMessage("Nota de Crédito solo puede referenciar DTEs tipo 03 (Crédito Fiscal) o 07 (Comprobante de Retención) según esquema MH");
            });

            // ===================================================================
            // VALIDACIONES NOTA DE DÉBITO (DTE-06)
            // ===================================================================
            When(x => x.Identificacion != null && x.Identificacion.TipoDte == "06", () =>
            {
                // ND requiere al menos 1 documento relacionado (máximo 50)
                RuleFor(x => x.DocumentosRelacionados)
                    .NotNull().WithMessage("Nota de Débito requiere al menos un documento relacionado")
                    .Must(dr => dr != null && dr.Count >= 1)
                    .WithMessage("Nota de Débito requiere al menos un documento relacionado")
                    .Must(dr => dr == null || dr.Count <= 50)
                    .WithMessage("No puede tener más de 50 documentos relacionados");

                // Validar cada documento relacionado
                RuleForEach(x => x.DocumentosRelacionados)
                    .SetValidator(new DocumentoRelacionadoDtoValidator());

                // Según esquema MH fe-nd-v3.json, solo permite tipoDocumento "03" (CCF) y "07" (CR)
                RuleForEach(x => x.DocumentosRelacionados)
                    .Must(dr => dr.TipoDocumento == "03" || dr.TipoDocumento == "07")
                    .WithMessage("Nota de Débito solo puede referenciar DTEs tipo 03 (Crédito Fiscal) o 07 (Comprobante de Retención) según esquema MH");
            });

            // ===================================================================
            // VALIDACIONES DE CONTINGENCIA
            // ===================================================================

            // MODIFICADO - TipoContingencia solo obligatorio si CrearEventoAutomatico = true
            When(x => x.Identificacion != null && x.Identificacion.TipoOperacion == 2 && x.Identificacion.CrearEventoAutomatico, () =>
            {
                RuleFor(x => x.Identificacion.TipoContingencia)
                    .NotNull().WithMessage("TipoContingencia es obligatorio cuando TipoOperacion es 2 y se crea evento automático")
                    .InclusiveBetween(1, 5).WithMessage("TipoContingencia debe estar entre 1 y 5");
            });

            // Si TipoContingencia == 5, MotivoContingencia es obligatorio
            When(x => x.Identificacion != null && x.Identificacion.TipoContingencia == 5, () =>
            {
                RuleFor(x => x.Identificacion.MotivoContingencia)
                    .NotEmpty().WithMessage("MotivoContingencia es obligatorio cuando TipoContingencia es 5 (Otro)")
                    .MaximumLength(150).WithMessage("MotivoContingencia no puede exceder 150 caracteres");
            });

            // Si TipoOperacion == 2, forzar TipoModelo == 2
            When(x => x.Identificacion != null && x.Identificacion.TipoOperacion == 2, () =>
            {
                RuleFor(x => x.Identificacion.TipoModelo)
                    .Equal(2).WithMessage("TipoModelo debe ser 2 (Diferido) cuando TipoOperacion es 2 (Contingencia)");
            });

            // ===================================================================
            // VALIDACIONES FSE (DTE-14) - Factura de Sujeto Excluido
            // ===================================================================
            RuleFor(x => x)
                .Custom((factura, context) =>
                {
                    if (factura.Identificacion == null || factura.Identificacion.TipoDte != "14"
                        || factura.CuerpoDocumento == null || factura.Resumen == null)
                        return;

                    // FSE requiere receptor (sujeto excluido)
                    if (!factura.ReceptorId.HasValue && factura.Receptor == null)
                    {
                        context.AddFailure("Receptor",
                            "FSE (tipo 14) requiere datos del sujeto excluido (Receptor o ReceptorId)");
                    }

                    // Validar campos obligatorios del sujeto excluido cuando se envía inline
                    if (factura.Receptor != null)
                    {
                        if (string.IsNullOrWhiteSpace(factura.Receptor.TipoDocumento))
                            context.AddFailure("Receptor.TipoDocumento", "TipoDocumento es obligatorio para sujeto excluido");

                        if (string.IsNullOrWhiteSpace(factura.Receptor.NumDocumento))
                            context.AddFailure("Receptor.NumDocumento", "NumDocumento es obligatorio para sujeto excluido");

                        if (string.IsNullOrWhiteSpace(factura.Receptor.Nombre))
                            context.AddFailure("Receptor.Nombre", "Nombre es obligatorio para sujeto excluido");

                        if (string.IsNullOrWhiteSpace(factura.Receptor.CodActividad))
                            context.AddFailure("Receptor.CodActividad", "CodActividad es obligatorio para sujeto excluido");

                        if (string.IsNullOrWhiteSpace(factura.Receptor.DescActividad))
                            context.AddFailure("Receptor.DescActividad", "DescripcionActividad es obligatorio para sujeto excluido");

                        // Dirección obligatoria en FSE
                        if (factura.Receptor.Direccion == null)
                        {
                            context.AddFailure("Receptor.Direccion", "Dirección es obligatoria para sujeto excluido");
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(factura.Receptor.Direccion.Departamento))
                                context.AddFailure("Receptor.Direccion.Departamento", "Departamento es obligatorio para sujeto excluido");
                            if (string.IsNullOrWhiteSpace(factura.Receptor.Direccion.Municipio))
                                context.AddFailure("Receptor.Direccion.Municipio", "Municipio es obligatorio para sujeto excluido");
                            if (string.IsNullOrWhiteSpace(factura.Receptor.Direccion.Complemento))
                                context.AddFailure("Receptor.Direccion.Complemento", "Complemento de dirección es obligatorio para sujeto excluido");
                        }
                    }

                    // Validar que todos los ítems tengan Compra
                    foreach (var item in factura.CuerpoDocumento)
                    {
                        if (!item.Compra.HasValue || item.Compra.Value <= 0)
                        {
                            context.AddFailure($"CuerpoDocumento[{item.NumItem - 1}].Compra",
                                $"Compra es obligatorio y debe ser > 0 para FSE (ítem #{item.NumItem})");
                        }
                    }

                    // Validar TotalCompras
                    decimal totalComprasCalculado = factura.CuerpoDocumento
                        .Sum(i => (i.Compra ?? 0) + (i.MontoDescuento ?? 0));
                    decimal totalComprasDto = factura.Resumen.TotalCompras ?? 0;
                    if (Math.Abs(totalComprasCalculado - totalComprasDto) > 0.01m)
                    {
                        context.AddFailure("Resumen.TotalCompras",
                            $"TotalCompras ({totalComprasDto:F2}) no coincide con SUM(compra + montoDescu) = {totalComprasCalculado:F2}");
                    }

                    // Validar totalDescu
                    decimal totalDescuCalculado = factura.CuerpoDocumento.Sum(i => i.MontoDescuento ?? 0);
                    decimal descuGlobal = factura.Resumen.Descu ?? 0;
                    decimal totalDescuDto = factura.Resumen.TotalDescu ?? 0;
                    if (Math.Abs(totalDescuCalculado - totalDescuDto) > 0.01m)
                    {
                        context.AddFailure("Resumen.TotalDescu",
                            $"TotalDescu ({totalDescuDto:F2}) no coincide con SUM(montoDescu) = {totalDescuCalculado:F2}");
                    }

                    // subTotal = totalCompras - totalDescu
                    decimal subTotalCalculado = totalComprasDto - totalDescuDto;
                    if (Math.Abs(subTotalCalculado - factura.Resumen.SubTotal) > 0.01m)
                    {
                        context.AddFailure("Resumen.SubTotal",
                            $"SubTotal ({factura.Resumen.SubTotal:F2}) no coincide con TotalCompras - TotalDescu = {subTotalCalculado:F2}");
                    }

                    // RN-004: reteRenta = subTotal * 0.10 si subTotal > 100
                    if (factura.Resumen.ReteRenta.HasValue && factura.Resumen.ReteRenta.Value > 0)
                    {
                        if (factura.Resumen.SubTotal <= 100.00m)
                        {
                            context.AddFailure("Resumen.ReteRenta",
                                "Retención de renta no aplica cuando SubTotal <= $100.00");
                        }
                        else
                        {
                            decimal reteRentaEsperada = Math.Round(factura.Resumen.SubTotal * 0.10m, 2);
                            if (Math.Abs(reteRentaEsperada - factura.Resumen.ReteRenta.Value) > 0.01m)
                            {
                                context.AddFailure("Resumen.ReteRenta",
                                    $"ReteRenta ({factura.Resumen.ReteRenta.Value:F2}) no coincide con SubTotal × 10% = {reteRentaEsperada:F2}");
                            }
                        }
                    }

                    // RN-005: totalPagar = subTotal - ivaRete1 - reteRenta
                    decimal totalPagarCalculado = factura.Resumen.SubTotal
                        - (factura.Resumen.IvaRete1 ?? 0)
                        - (factura.Resumen.ReteRenta ?? 0);
                    if (Math.Abs(totalPagarCalculado - factura.Resumen.TotalPagar) > 0.01m)
                    {
                        context.AddFailure("Resumen.TotalPagar",
                            $"TotalPagar ({factura.Resumen.TotalPagar:F2}) no coincide con SubTotal - IvaRete1 - ReteRenta = {totalPagarCalculado:F2}");
                    }

                    // Observaciones: máximo 3000 caracteres
                    if (factura.Observaciones != null && factura.Observaciones.Length > 3000)
                    {
                        context.AddFailure("Observaciones", "Observaciones no puede exceder 3000 caracteres");
                    }
                });

            // ===================================================================
            // VALIDACIONES DE FÓRMULAS DEL RESUMEN SEGÚN MINISTERIO DE HACIENDA
            // Soporta FACTURA (DTE-01) y CCF (DTE-03) con fórmulas diferenciadas
            // ===================================================================
            RuleFor(x => x)
                .Custom((factura, context) =>
                {
                    if (factura.Identificacion != null && factura.CuerpoDocumento != null && factura.Resumen != null)
                    {
                        // Saltar validaciones FCF/CCF para FSE (tipo 14), NC (tipo 05) y ND (tipo 06) — se validan por separado
                        if (factura.Identificacion.TipoDte == "14" || factura.Identificacion.TipoDte == "05" || factura.Identificacion.TipoDte == "06") return;

                        bool esFactura = factura.Identificacion.TipoDte == "01";
                        bool esCCF = factura.Identificacion.TipoDte == "03";

                        // Validar Total Gravado
                        decimal totalGravadoCalculado = factura.CuerpoDocumento.Sum(i => i.VentaGravada);
                        if (Math.Abs(totalGravadoCalculado - factura.Resumen.TotalGravada) > 0.01m)
                        {
                            context.AddFailure("Resumen.TotalGravada",
                                $"El total gravado del resumen ({factura.Resumen.TotalGravada:F2}) no coincide con la suma de ítems ({totalGravadoCalculado:F2})");
                        }

                        // Validar Total Exento
                        decimal totalExentoCalculado = factura.CuerpoDocumento.Sum(i => i.VentaExenta);
                        if (Math.Abs(totalExentoCalculado - factura.Resumen.TotalExenta) > 0.01m)
                        {
                            context.AddFailure("Resumen.TotalExenta",
                                $"El total exento del resumen ({factura.Resumen.TotalExenta:F2}) no coincide con la suma de ítems ({totalExentoCalculado:F2})");
                        }

                        // Validar Total No Sujeto
                        decimal totalNoSujCalculado = factura.CuerpoDocumento.Sum(i => i.VentaNoSuj);
                        if (Math.Abs(totalNoSujCalculado - factura.Resumen.TotalNoSuj) > 0.01m)
                        {
                            context.AddFailure("Resumen.TotalNoSuj",
                                $"El total no sujeto del resumen ({factura.Resumen.TotalNoSuj:F2}) no coincide con la suma de ítems ({totalNoSujCalculado:F2})");
                        }

                        // =========================================================================
                        // FÓRMULA 2 (Ítem): Validar IVA de cada ítem según tipo de documento
                        // =========================================================================
                        foreach (var item in factura.CuerpoDocumento.Where(i => i.VentaGravada > 0))
                        {
                            decimal ivaCalculado;
                            string formulaUsada;

                            if (esFactura)
                            {
                                // FACTURA: ivaItem = (ventaGravada / 1.13) × 0.13 (ingeniería inversa)
                                ivaCalculado = Math.Round((item.VentaGravada / 1.13m) * 0.13m, 2);
                                formulaUsada = "(VentaGravada / 1.13) × 0.13";
                            }
                            else // CCF
                            {
                                // CCF: ivaItem = ventaGravada × 0.13 (directo)
                                ivaCalculado = Math.Round(item.VentaGravada * 0.13m, 2);
                                formulaUsada = "VentaGravada × 0.13";
                            }

                            if (Math.Abs(ivaCalculado - item.IvaItem) > 0.01m)
                            {
                                string tipoDoc = esFactura ? "FACTURA" : "CCF";
                                context.AddFailure($"CuerpoDocumento[{item.NumItem - 1}].IvaItem",
                                    $"[{tipoDoc}] IvaItem del ítem #{item.NumItem} ({item.IvaItem:F2}) no coincide con el cálculo esperado " +
                                    $"({formulaUsada} = {ivaCalculado:F2})");
                            }
                        }

                        // FÓRMULA 5: subTotalVentas = totalGravada + totalExenta + totalNoSuj
                        decimal subTotalVentasCalculado = totalGravadoCalculado + totalExentoCalculado + totalNoSujCalculado;
                        if (factura.Resumen.SubTotalVentas.HasValue)
                        {
                            if (Math.Abs(subTotalVentasCalculado - factura.Resumen.SubTotalVentas.Value) > 0.01m)
                            {
                                context.AddFailure("Resumen.SubTotalVentas",
                                    $"El subtotal de ventas ({factura.Resumen.SubTotalVentas.Value:F2}) no coincide con el esperado ({subTotalVentasCalculado:F2})");
                            }
                        }

                        // =========================================================================
                        // FÓRMULA 3: ivaDescuentoGlobal según tipo de documento
                        // =========================================================================
                        decimal sumaIvaItems = factura.CuerpoDocumento.Sum(i => i.IvaItem);
                        decimal ivaDescuentoGlobal = 0m;

                        if (factura.Resumen.DescuGravada.HasValue && factura.Resumen.DescuGravada.Value > 0)
                        {
                            if (esFactura)
                            {
                                // FACTURA: ivaDescuentoGlobal = (descuGravada / 1.13) × 0.13
                                ivaDescuentoGlobal = Math.Round((factura.Resumen.DescuGravada.Value / 1.13m) * 0.13m, 2);
                            }
                            else // CCF
                            {
                                // CCF: ivaDescuentoGlobal = descuGravada × 0.13
                                ivaDescuentoGlobal = Math.Round(factura.Resumen.DescuGravada.Value * 0.13m, 2);
                            }
                        }

                        // FÓRMULA 4: totalIva = Σ(ivaItem) - ivaDescuentoGlobal
                        // IMPORTANTE: Σ(ivaItem) ≠ totalIva cuando hay descuento global (ES CORRECTO según MH)
                        decimal totalIvaEsperado = sumaIvaItems - ivaDescuentoGlobal;
                        if (Math.Abs(totalIvaEsperado - factura.Resumen.TotalIva) > 0.01m)
                        {
                            string tipoDoc = esFactura ? "FACTURA" : "CCF";
                            context.AddFailure("Resumen.TotalIva",
                                $"[{tipoDoc}] El total IVA ({factura.Resumen.TotalIva:F2}) no coincide con el cálculo esperado " +
                                $"(Σ IvaItems [{sumaIvaItems:F2}] - IVA Descuento Global [{ivaDescuentoGlobal:F2}] = {totalIvaEsperado:F2})");
                        }

                        // FÓRMULA 6: subTotal = subTotalVentas - (descuGravada + descuExenta + descuNoSuj)
                        decimal descuentosTotales = (factura.Resumen.DescuGravada ?? 0m)
                                                  + (factura.Resumen.DescuExenta ?? 0m)
                                                  + (factura.Resumen.DescuNoSuj ?? 0m);
                        decimal subTotalCalculado = subTotalVentasCalculado - descuentosTotales;

                        if (Math.Abs(subTotalCalculado - factura.Resumen.SubTotal) > 0.01m)
                        {
                            context.AddFailure("Resumen.SubTotal",
                                $"El subtotal ({factura.Resumen.SubTotal:F2}) no coincide con el esperado " +
                                $"(SubTotalVentas [{subTotalVentasCalculado:F2}] - Descuentos [{descuentosTotales:F2}] = {subTotalCalculado:F2})");
                        }

                        // =========================================================================
                        // FÓRMULA 7: Retención Renta según tipo de documento
                        // =========================================================================
                        decimal reteRentaCalculada = 0m;

                        if (esFactura)
                        {
                            // FACTURA: reteRenta = [(Σ ServiciosGravados / 1.13) + Σ ServiciosExentos] × 0.10
                            decimal baseImponibleRenta = 0m;
                            foreach (var item in factura.CuerpoDocumento.Where(i => i.TipoItem == 2)) // Solo Servicios
                            {
                                if (item.VentaGravada > 0)
                                {
                                    // Servicio gravado: quitar el IVA para obtener la base
                                    baseImponibleRenta += (item.VentaGravada / 1.13m);
                                }
                                else if (item.VentaExenta > 0)
                                {
                                    // Servicio exento: el total es la base (no tiene IVA)
                                    baseImponibleRenta += item.VentaExenta;
                                }
                            }
                            reteRentaCalculada = Math.Round(baseImponibleRenta * 0.10m, 2);
                        }
                        else if (esCCF)
                        {
                            // CCF: reteRenta = subTotal × 0.10 (sobre TODO el subtotal si es servicio)
                            // Solo aplica si TODOS los ítems son servicios
                            bool todosServicios = factura.CuerpoDocumento.All(i => i.TipoItem == 2);
                            if (todosServicios)
                            {
                                reteRentaCalculada = Math.Round(subTotalCalculado * 0.10m, 2);
                            }
                        }

                        if (factura.Resumen.ReteRenta.HasValue)
                        {
                            if (Math.Abs(reteRentaCalculada - factura.Resumen.ReteRenta.Value) > 0.01m)
                            {
                                string tipoDoc = esFactura ? "FACTURA" : "CCF";
                                string formula = esFactura
                                    ? "[(Σ ServiciosGravados / 1.13) + Σ ServiciosExentos] × 10%"
                                    : "SubTotal × 10% (solo si todos los ítems son servicios)";

                                context.AddFailure("Resumen.ReteRenta",
                                    $"[{tipoDoc}] La retención de renta ({factura.Resumen.ReteRenta.Value:F2}) no coincide con el cálculo esperado " +
                                    $"({formula} = {reteRentaCalculada:F2})");
                            }
                        }

                        // Validar que si hay servicios con base > 0, ReteRenta no sea null
                        // MODIFICACION: Se hace opcional a petición del usuario. Si no viene, se asume 0 y no se retiene.
                        /*
                        if (reteRentaCalculada > 0 && !factura.Resumen.ReteRenta.HasValue)
                        {
                            context.AddFailure("Resumen.ReteRenta",
                                $"Debe especificar ReteRenta ya que el cálculo esperado es ${reteRentaCalculada:F2}.");
                        }
                        */

                        // Validar totalNoGravado (suma de noGravado de ítems)
                        decimal totalNoGravadoCalculado = factura.CuerpoDocumento
                            .Where(i => i.NoGravado.HasValue)
                            .Sum(i => i.NoGravado!.Value);

                        if (factura.Resumen.TotalNoGravado.HasValue)
                        {
                            if (Math.Abs(totalNoGravadoCalculado - factura.Resumen.TotalNoGravado.Value) > 0.01m)
                            {
                                context.AddFailure("Resumen.TotalNoGravado",
                                    $"El total no gravado ({factura.Resumen.TotalNoGravado.Value:F2}) no coincide con la suma de ítems noGravado ({totalNoGravadoCalculado:F2})");
                            }
                        }

                        // =========================================================================
                        // FÓRMULA 8: montoTotalOperacion según tipo de documento
                        // =========================================================================
                        decimal totalTributosSec1 = 0m;
                        if (factura.Resumen.Tributos != null && factura.Resumen.Tributos.Any())
                        {
                            totalTributosSec1 = factura.Resumen.Tributos.Sum(t => t.Valor);
                        }

                        decimal montoTotalOperacionCalculado;

                        if (esFactura)
                        {
                            // FACTURA: montoTotalOperacion = subTotal + totalTributosSec1
                            // (El IVA ya está incluido en subTotal)
                            montoTotalOperacionCalculado = subTotalCalculado + totalTributosSec1;
                        }
                        else // CCF
                        {
                            // CCF según MH Campo 146: montoTotalOperacion = subTotal + Σ(tributos.valor)
                            // El IVA ('20') viene incluido en los tributos, NO se suma por separado
                            montoTotalOperacionCalculado = subTotalCalculado + totalTributosSec1;
                        }

                        if (factura.Resumen.MontoTotalOperacion.HasValue)
                        {
                            if (Math.Abs(montoTotalOperacionCalculado - factura.Resumen.MontoTotalOperacion.Value) > 0.01m)
                            {
                                string tipoDoc = esFactura ? "FACTURA" : "CCF";
                                string formula = $"SubTotal [{subTotalCalculado:F2}] + Tributos [{totalTributosSec1:F2}]";

                                context.AddFailure("Resumen.MontoTotalOperacion",
                                    $"[{tipoDoc}] El monto total de operación ({factura.Resumen.MontoTotalOperacion.Value:F2}) no coincide con el esperado " +
                                    $"({formula} = {montoTotalOperacionCalculado:F2})");
                            }
                        }

                        // FÓRMULA 9: totalPagar = montoTotalOperacion + totalNoGravado - ivaRete1 + ivaPerci1 - reteRenta
                        decimal totalPagarCalculado = montoTotalOperacionCalculado
                                                    + (factura.Resumen.TotalNoGravado ?? 0m)
                                                    - (factura.Resumen.IvaRete1 ?? 0m)
                                                    + (factura.Resumen.IvaPerci1 ?? 0m)
                                                    - (factura.Resumen.ReteRenta ?? 0m);

                        if (Math.Abs(totalPagarCalculado - factura.Resumen.TotalPagar) > 0.01m)
                        {
                            context.AddFailure("Resumen.TotalPagar",
                                $"El total a pagar ({factura.Resumen.TotalPagar:F2}) no coincide con el cálculo esperado " +
                                $"(MontoTotalOperacion [{montoTotalOperacionCalculado:F2}] + NoGravado [{factura.Resumen.TotalNoGravado ?? 0m:F2}] " +
                                $"- IvaRete [{factura.Resumen.IvaRete1 ?? 0m:F2}] + IvaPerci [{factura.Resumen.IvaPerci1 ?? 0m:F2}] - ReteRenta [{factura.Resumen.ReteRenta ?? 0m:F2}] = {totalPagarCalculado:F2})");
                        }
                    }
                });

            // Validar que si se proporciona ReceptorId, no se envíe Receptor
            RuleFor(x => x)
                .Must(x => !(x.ReceptorId.HasValue && x.Receptor != null))
                .WithMessage("No puede especificar ReceptorId y Receptor al mismo tiempo. Use uno u otro");

            // REGULACIÓN MH: Receptor OBLIGATORIO si TotalPagar >= $1,095.00 (no aplica a FSE, ya validado arriba)
            RuleFor(x => x)
                .Must(x => x.ReceptorId.HasValue || x.Receptor != null)
                .When(x => x.Resumen != null && x.Resumen.TotalPagar >= 1095.00m
                    && x.Identificacion != null && x.Identificacion.TipoDte != "14")
                .WithMessage("Receptor es OBLIGATORIO cuando el total a pagar es >= $1,095.00 según regulación del Ministerio de Hacienda");

            // VentaTercero (específico CCF)
            RuleFor(x => x.VentaTercero)
                .SetValidator(new VentaTerceroDtoValidator()!)
                .When(x => x.VentaTercero != null);

            // OtrosDocumentos (específico CCF, máximo 10)
            RuleFor(x => x.OtrosDocumentos)
                .Must(list => list == null || list.Count <= 10)
                .WithMessage("No puede haber más de 10 documentos asociados");

            RuleForEach(x => x.OtrosDocumentos)
                .SetValidator(new OtroDocumentoDtoValidator())
                .When(x => x.OtrosDocumentos != null && x.OtrosDocumentos.Any());

            // Extension (información de entrega)
            RuleFor(x => x.Extension)
                .SetValidator(new ExtensionDtoValidator()!)
                .When(x => x.Extension != null);
        }
    }
}

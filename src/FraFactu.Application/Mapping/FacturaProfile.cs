using AutoMapper;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Domain.Entities;

namespace FraFactu.Application.Mapping
{
    /// <summary>
    /// Perfil de AutoMapper para Facturas Electrónicas
    /// </summary>
    public class FacturaProfile : Profile
    {
        public FacturaProfile()
        {
            // FacturaElectronica -> FacturaElectronicaResponseDto
            CreateMap<FacturaElectronica, FacturaElectronicaResponseDto>()
                .ForMember(dest => dest.Identificacion, opt => opt.MapFrom(src => new IdentificacionDto
                {
                    Version = src.Version,
                    // Ambiente se obtiene del emisor asociado, ya no se almacena en la factura
                    TipoDte = src.TipoDocumento != null ? src.TipoDocumento.Codigo : "01",
                    NumeroControl = src.NumeroControl,
                    CodigoGeneracion = src.CodigoGeneracion,
                    TipoModelo = src.CatModeloFacturacionId,
                    TipoOperacion = src.CatTipoTransmisionId,
                    FechaEmision = src.FechaEmision,
                    HoraEmision = src.HoraEmision.ToString(@"hh\:mm\:ss"),
                    TipoMoneda = "USD"
                }))
                .ForMember(dest => dest.Emisor, opt => opt.MapFrom((src, dest, destMember, context) => new EmisorDteDto
                {
                    Nit = src.Emisor.Nit,
                    Nrc = src.Emisor.Nrc,
                    Nombre = src.Emisor.NombreRazonSocial,
                    CodActividad = src.Emisor.CodigoActividad,
                    DescActividad = src.Emisor.DescripcionActividad,
                    NombreComercial = src.Emisor.NombreComercial,
                    Telefono = src.Sucursal?.Telefono ?? src.Emisor.Telefono,
                    Correo = src.Sucursal?.CorreoElectronico ?? src.Emisor.CorreoElectronico,
                    TipoEstablecimiento = src.Sucursal?.TipoEstablecimiento?.Codigo!,
                    Direccion = new DireccionDto
                    {
                        Departamento = src.Sucursal?.Departamento?.Codigo ?? src.Emisor.Departamento.Codigo,
                        Municipio = src.Sucursal?.Municipio?.Codigo ?? src.Emisor.Municipio.Codigo,
                        Complemento = src.Sucursal?.Direccion ?? src.Emisor.Direccion
                    },
                    CodEstablecimiento = src.Sucursal?.CodigoEstablecimiento,
                    CodPuntoVenta = src.Caja?.CodPuntoVentaMH
                }))
                .ForMember(dest => dest.Receptor, opt => opt.MapFrom(src => src.Receptor != null ? new ReceptorDteDto
                {
                    // Receptor FC "Sin documento" → TipoDocumento+NumDocumento null (MH acepta en FC tipo 01).
                    TipoDocumento = src.Receptor.TipoDocumento != null ? src.Receptor.TipoDocumento.Codigo : null,
                    NumDocumento = src.Receptor.NumeroDocumento,
                    Nrc = src.Receptor.Nrc,
                    Nombre = src.Receptor.NombreRazonSocial,
                    CodActividad = src.Receptor.CodigoActividad,
                    DescActividad = src.Receptor.DescripcionActividad,
                    Direccion = src.Receptor.Departamento != null ? new DireccionDto
                    {
                        Departamento = src.Receptor.Departamento.Codigo,
                        Municipio = src.Receptor.Municipio != null ? src.Receptor.Municipio.Codigo : "",
                        Complemento = src.Receptor.Direccion
                    } : null,
                    Telefono = src.Receptor.Telefono,
                    Correo = src.Receptor.CorreoElectronico
                } : null))
                .ForMember(dest => dest.CuerpoDocumento, opt => opt.MapFrom(src => src.Detalles))
                .ForMember(dest => dest.Resumen, opt => opt.MapFrom(src => new ResumenDto
                {
                    TotalNoSuj = src.TotalNoSujeto,
                    TotalExenta = src.TotalExento,
                    TotalGravada = src.TotalGravado,
                    TotalDescu = src.TotalDescuento,
                    TotalIva = src.TotalIva,
                    SubTotal = src.SubTotal,
                    IvaRete1 = src.IvaRetenido,
                    ReteRenta = src.RetencionRenta,
                    TotalPagar = src.TotalPagar,
                    TotalLetras = src.TotalLetras,
                    TotalIvaPerc = src.IvaPercibido,
                    MontoTotalOperacion = src.MontoTotalOperacion,
                    CondicionOperacion = src.CatCondicionOperacionId,
                    Pagos = src.Pagos.Select(p => new PagoDto
                    {
                        Id = p.Id,
                        CatFormaPagoId = p.CatFormaPagoId,
                        Monto = p.Monto,
                        Referencia = p.Referencia,
                        CatPlazoId = p.CatPlazoId,
                        Periodo = p.Periodo
                    }).ToList(),
                    NumPagoElectronico = src.NumPagoElectronico,
                    Tributos = src.Tributos != null && src.Tributos.Any()
                        ? src.Tributos.Select(t => new TributoResumenDto
                        {
                            Codigo = t.CodigoAttribute,
                            Descripcion = t.Descripcion,
                            Valor = t.Valor
                        }).ToList()
                        : null
                }))
                .ForMember(dest => dest.SucursalId, opt => opt.MapFrom(src => src.SucursalId))
                .ForMember(dest => dest.VendedorId, opt => opt.MapFrom(src => src.VendedorId))
                .ForMember(dest => dest.VendedorNombre, opt => opt.MapFrom(src => src.Vendedor != null ? src.Vendedor.Nombre : null))
                .ForMember(dest => dest.VendedorCodigo, opt => opt.MapFrom(src => src.Vendedor != null ? src.Vendedor.Codigo : null))
                .ForMember(dest => dest.CajaId, opt => opt.MapFrom(src => src.CajaId))
                .ForMember(dest => dest.CajaCodigo, opt => opt.MapFrom(src => src.Caja != null ? src.Caja.Codigo : null))
                .ForMember(dest => dest.VentaTercero, opt => opt.MapFrom(src => src.VentaTercero))
                .ForMember(dest => dest.OtrosDocumentos, opt => opt.MapFrom(src => src.OtrosDocumentos))
                .ForMember(dest => dest.DocumentosRelacionados, opt => opt.MapFrom(src =>
                    src.DocumentosRelacionados != null && src.DocumentosRelacionados.Any()
                        ? src.DocumentosRelacionados.Select(d => new DocumentoRelacionadoResponseDto
                        {
                            TipoDocumento = d.TipoDocumento != null ? d.TipoDocumento.Valor : string.Empty,
                            NumeroDocumento = d.NumeroDocumento,
                            FechaEmision = d.FechaEmision
                        }).ToList()
                        : null))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.EstadoHacienda))
                .ForMember(dest => dest.SelloRecepcion, opt => opt.MapFrom(src => src.SelloRecibido))
                .ForMember(dest => dest.Extension, opt => opt.MapFrom(src => src.Extension != null ? new ExtensionDto
                {
                    NombEntrega = src.Extension.NombEntrega,
                    DocuEntrega = src.Extension.DocuEntrega,
                    NombRecibe = src.Extension.NombRecibe,
                    DocuRecibe = src.Extension.DocuRecibe,
                    PlacaVehiculo = src.Extension.PlacaVehiculo,
                    Observaciones = src.Extension.Observaciones
                } : null))
                .ForMember(dest => dest.Apendice, opt => opt.MapFrom(src =>
                    src.Apendices != null && src.Apendices.Any()
                        ? src.Apendices.Select(a => new ApendiceDto
                        {
                            Campo = a.Campo,
                            Etiqueta = a.Etiqueta,
                            Valor = a.Valor
                        }).ToList()
                        : null))
                .ForMember(dest => dest.JsonDte, opt => opt.Ignore())
                .ForMember(dest => dest.RespuestaHacienda, opt => opt.Ignore());

            // Sub-maps
            CreateMap<VentaTercero, VentaTerceroDto>();
            CreateMap<OtroDocumento, OtroDocumentoDto>();
            CreateMap<MedicoServicio, MedicoDto>();



            // FacturaElectronicaDetalle -> ItemDocumentoDto
            CreateMap<FacturaElectronicaDetalle, ItemDocumentoDto>()
                .ForMember(dest => dest.NumItem, opt => opt.MapFrom(src => src.NumeroItem))
                .ForMember(dest => dest.TipoItem, opt => opt.MapFrom(src => src.CatTipoItemId))
                .ForMember(dest => dest.NumeroDocumento, opt => opt.MapFrom(src => src.CodigoProducto))
                .ForMember(dest => dest.Codigo, opt => opt.MapFrom(src => src.CodigoProducto))
                .ForMember(dest => dest.UniMedida, opt => opt.MapFrom(src => src.CatUnidadMedidaId))
                .ForMember(dest => dest.PrecioUni, opt => opt.MapFrom(src => src.PrecioUnitario))
                .ForMember(dest => dest.MontoDescuento, opt => opt.MapFrom(src => src.MontoDescuento))
                .ForMember(dest => dest.VentaNoSuj, opt => opt.MapFrom(src => src.VentaNoSujeta))
                .ForMember(dest => dest.CodTributo, opt => opt.Ignore())
                .ForMember(dest => dest.Tributos, opt => opt.Ignore())
                .ForMember(dest => dest.Ieps, opt => opt.Ignore());

            // FacturaElectronica -> FacturaListDto (para listados)
            CreateMap<FacturaElectronica, FacturaListDto>()
                .ForMember(dest => dest.ReceptorNombre, opt => opt.MapFrom(src => src.Receptor != null ? src.Receptor.NombreRazonSocial : ""))
                .ForMember(dest => dest.ReceptorNumeroDocumento, opt => opt.MapFrom(src => src.Receptor != null ? src.Receptor.NumeroDocumento : ""))
                .ForMember(dest => dest.EstadoHacienda, opt => opt.MapFrom(src => src.EstadoHacienda))
                .ForMember(dest => dest.SucursalId, opt => opt.MapFrom(src => src.SucursalId));
        }
    }
}

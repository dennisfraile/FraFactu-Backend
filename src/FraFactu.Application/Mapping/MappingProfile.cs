using AutoMapper;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Emisores;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.DTOs.Permisos;
using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Application.DTOs.Receptores;
using FraFactu.Application.DTOs.Roles;
using FraFactu.Application.DTOs.Sucursales;
using FraFactu.Application.DTOs.Usuarios;
using FraFactu.Application.DTOs.Suscripciones;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Application.Mapping
{
    /// <summary>
    /// Perfil de configuración de AutoMapper para todos los mapeos del sistema
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ==========================================
            // CATÁLOGOS (Genérico para todos)
            // ==========================================
            CreateMap<CatalogoBase, CatalogoDto>();
            CreateMap<CatTipoDocumento, CatalogoDto>();
            CreateMap<CatTipoItem, CatalogoDto>();
            CreateMap<CatUnidadMedida, CatalogoDto>();
            CreateMap<CatFormaPago, CatalogoDto>();
            CreateMap<CatPlazo, CatalogoDto>();
            CreateMap<CatTributo, CatalogoDto>();
            // ... (todos los demás catálogos heredan de CatalogoBase)

            // USUARIOS: mapeo manual en UsuarioService (MapToDto/MapToListDto)
            // por la relación many-to-many con sucursales

            // ==========================================
            // EMISORES
            // ==========================================
            CreateMap<Emisor, EmisorDto>()
                .ForMember(dest => dest.TotalUsuarios, opt => opt.MapFrom(src => src.Usuarios.Count))
                .ForMember(dest => dest.TotalFacturas, opt => opt.MapFrom(src => src.Facturas.Count))
                .ForMember(dest => dest.DepartamentoNombre, opt => opt.MapFrom(src => src.Departamento.Valor))
                .ForMember(dest => dest.MunicipioNombre, opt => opt.MapFrom(src => src.Municipio.Valor))
                .ForMember(dest => dest.DistritoNombre, opt => opt.MapFrom(src => src.Distrito != null ? src.Distrito.Valor : null))
                .ForMember(dest => dest.TipoEstablecimientoNombre, opt => opt.MapFrom(src => src.TipoEstablecimiento != null ? src.TipoEstablecimiento.Valor : null))
                // Mapear ambiente del catálogo
                .ForMember(dest => dest.AmbienteDestinoCodigo, opt => opt.MapFrom(src => src.AmbienteDestino.Codigo))
                // SMTP: campo computado
                .ForMember(dest => dest.SmtpConfigurado, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.SmtpUser) && !string.IsNullOrEmpty(src.SmtpPassword)))
                // Gmail OAuth2
                .ForMember(dest => dest.GmailEmail, opt => opt.MapFrom(src => src.GmailEmail))
                .ForMember(dest => dest.GmailConectado, opt => opt.MapFrom(src => src.GmailConectado));

            CreateMap<CreateEmisorDto, Emisor>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.Usuarios, opt => opt.Ignore())
                .ForMember(dest => dest.Facturas, opt => opt.Ignore())
                .ForMember(dest => dest.Departamento, opt => opt.Ignore())
                .ForMember(dest => dest.Municipio, opt => opt.Ignore())
                .ForMember(dest => dest.TipoEstablecimiento, opt => opt.Ignore());

            CreateMap<UpdateEmisorDto, Emisor>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Usuarios, opt => opt.Ignore())
                .ForMember(dest => dest.Facturas, opt => opt.Ignore())
                .ForMember(dest => dest.Departamento, opt => opt.Ignore())
                .ForMember(dest => dest.Municipio, opt => opt.Ignore())
                .ForMember(dest => dest.TipoEstablecimiento, opt => opt.Ignore());

            // Mapeo de Emisor a EmisorDteDto (para FacturaElectronicaResponseDto)
            CreateMap<Emisor, EmisorDteDto>()
                .ForMember(dest => dest.Nit, opt => opt.MapFrom(src => src.Nit))
                .ForMember(dest => dest.Nrc, opt => opt.MapFrom(src => src.Nrc))
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.NombreRazonSocial))
                .ForMember(dest => dest.CodActividad, opt => opt.MapFrom(src => src.CodigoActividad))
                .ForMember(dest => dest.DescActividad, opt => opt.MapFrom(src => src.DescripcionActividad))
                .ForMember(dest => dest.NombreComercial, opt => opt.MapFrom(src => src.NombreComercial))
                .ForMember(dest => dest.TipoEstablecimiento, opt => opt.MapFrom(src => src.TipoEstablecimiento!.Codigo))
                .ForMember(dest => dest.Direccion, opt => opt.Ignore()) // Mapear manualmente si es necesario
                .ForMember(dest => dest.Telefono, opt => opt.MapFrom(src => src.Telefono))
                .ForMember(dest => dest.Correo, opt => opt.MapFrom(src => src.CorreoElectronico))
                .ForMember(dest => dest.CodEstablecimiento, opt => opt.Ignore())
                .ForMember(dest => dest.CodPuntoVenta, opt => opt.Ignore());


            // ==========================================
            // RECEPTORES (Clientes)
            // ==========================================
            CreateMap<Receptor, ReceptorDto>()
                .ForMember(dest => dest.EmisorNombre, opt => opt.MapFrom(src => src.Emisor != null ? src.Emisor.NombreRazonSocial : null))
                .ForMember(dest => dest.TipoDocumentoNombre, opt => opt.MapFrom(src => src.TipoDocumento != null ? src.TipoDocumento.Valor : "Sin documento"))
                .ForMember(dest => dest.DepartamentoNombre, opt => opt.MapFrom(src => src.Departamento != null ? src.Departamento.Valor : null))
                .ForMember(dest => dest.MunicipioNombre, opt => opt.MapFrom(src => src.Municipio != null ? src.Municipio.Valor : null))
                .ForMember(dest => dest.DistritoNombre, opt => opt.MapFrom(src => src.Distrito != null ? src.Distrito.Valor : null))
                .ForMember(dest => dest.TotalFacturas, opt => opt.MapFrom(src => src.Facturas.Count))
                .ForMember(dest => dest.TotalFacturado, opt => opt.MapFrom(src => src.Facturas.Sum(f => f.TotalPagar)))
                // Mapeo explícito: Entidad.CatTipoDocumentoIdentificacionReceptorId -> DTO.CatTipoDocumentoId
                .ForMember(dest => dest.CatTipoDocumentoId, opt => opt.MapFrom(src => src.CatTipoDocumentoIdentificacionReceptorId));

            CreateMap<Receptor, ReceptorListDto>()
                // Mapeo explícito: Entidad.CatTipoDocumentoIdentificacionReceptorId -> DTO.CatTipoDocumentoId
                .ForMember(dest => dest.CatTipoDocumentoId, opt => opt.MapFrom(src => src.CatTipoDocumentoIdentificacionReceptorId))
                 .ForMember(dest => dest.TipoDocumentoNombre,
                    opt => opt.MapFrom(src => src.TipoDocumento != null ? src.TipoDocumento.Valor : "Sin documento"));

            CreateMap<CreateReceptorDto, Receptor>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore()) // Se establece desde el contexto
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.Facturas, opt => opt.Ignore())
                .ForMember(dest => dest.TipoDocumento, opt => opt.Ignore())
                .ForMember(dest => dest.Departamento, opt => opt.Ignore())
                .ForMember(dest => dest.Municipio, opt => opt.Ignore())
                .ForMember(dest => dest.Telefono, opt => opt.NullSubstitute(string.Empty))
                // Mapeo explícito: DTO.CatTipoDocumentoId -> Entidad.CatTipoDocumentoIdentificacionReceptorId
                .ForMember(dest => dest.CatTipoDocumentoIdentificacionReceptorId, opt => opt.MapFrom(src => src.CatTipoDocumentoId));

            CreateMap<UpdateReceptorDto, Receptor>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore())
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.Facturas, opt => opt.Ignore())
                .ForMember(dest => dest.TipoDocumento, opt => opt.Ignore())
                .ForMember(dest => dest.Departamento, opt => opt.Ignore())
                .ForMember(dest => dest.Municipio, opt => opt.Ignore())
                .ForMember(dest => dest.Telefono, opt => opt.NullSubstitute(string.Empty))
                // Mapeo explícito: DTO.CatTipoDocumentoId -> Entidad.CatTipoDocumentoIdentificacionReceptorId
                .ForMember(dest => dest.CatTipoDocumentoIdentificacionReceptorId, opt => opt.MapFrom(src => src.CatTipoDocumentoId));

            // ==========================================
            // ROLES Y PERMISOS
            // ==========================================
            CreateMap<Permiso, PermisoDto>();

            CreateMap<Rol, RolDto>()
                .ForMember(dest => dest.Permisos, opt => opt.MapFrom(src =>
                    src.RolesPermisos.Select(rp => rp.Permiso)))
                .ForMember(dest => dest.TotalUsuarios, opt => opt.MapFrom(src => src.Usuarios.Count));

            CreateMap<CreateRolDto, Rol>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.RolesPermisos, opt => opt.Ignore()) // Se manejan en el servicio
                .ForMember(dest => dest.Usuarios, opt => opt.Ignore());

            CreateMap<UpdateRolDto, Rol>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.RolesPermisos, opt => opt.Ignore())
                .ForMember(dest => dest.Usuarios, opt => opt.Ignore());

            // ==========================================
            // FACTURAS - COMPONENTES
            // ==========================================

            // Detalles de Factura
            CreateMap<FacturaElectronicaDetalle, FacturaDetalleDto>()
                .ForMember(dest => dest.TipoItem, opt => opt.MapFrom(src => src.TipoItem))
                .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.UnidadMedida));

            CreateMap<CreateFacturaDetalleDto, FacturaElectronicaDetalle>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.FacturaId, opt => opt.Ignore())
                .ForMember(dest => dest.Factura, opt => opt.Ignore())
                .ForMember(dest => dest.TipoItem, opt => opt.Ignore())
                .ForMember(dest => dest.UnidadMedida, opt => opt.Ignore())
                .ForMember(dest => dest.IvaItem, opt => opt.Ignore()); // Se calcula en el servicio

            // Tributos de Factura
            CreateMap<FacturaTributo, FacturaTributoDto>()
                .ForMember(dest => dest.Tributo, opt => opt.MapFrom(src => src.Tributo));

            // Pagos
            CreateMap<Pago, PagoDto>()
                .ForMember(dest => dest.FormaPago, opt => opt.MapFrom(src => src.FormaPago))
                .ForMember(dest => dest.Plazo, opt => opt.MapFrom(src => src.CatPlazoId.HasValue ? src.FormaPago : null));

            CreateMap<CreatePagoDto, Pago>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.FacturaId, opt => opt.Ignore())
                .ForMember(dest => dest.Factura, opt => opt.Ignore())
                .ForMember(dest => dest.FormaPago, opt => opt.Ignore());

            // ==========================================
            // FACTURAS - PRINCIPAL
            // ==========================================
            CreateMap<FacturaElectronica, FacturaElectronicaDto>()
                // Emisor
                .ForMember(dest => dest.EmisorNit, opt => opt.MapFrom(src => src.Emisor != null ? src.Emisor.Nit : null))
                .ForMember(dest => dest.EmisorNombre, opt => opt.MapFrom(src => src.Emisor != null ? src.Emisor.NombreRazonSocial : null))
                // Receptor
                .ForMember(dest => dest.ReceptorNumeroDocumento, opt => opt.MapFrom(src => src.Receptor != null ? src.Receptor.NumeroDocumento : null))
                .ForMember(dest => dest.ReceptorNombre, opt => opt.MapFrom(src => src.Receptor != null ? src.Receptor.NombreRazonSocial : null))
                // Catálogo Tipo Documento
                .ForMember(dest => dest.TipoDocumento, opt => opt.MapFrom(src => src.TipoDocumento))
                // Listas
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles))
                .ForMember(dest => dest.Tributos, opt => opt.MapFrom(src => src.Tributos))
                .ForMember(dest => dest.Pagos, opt => opt.MapFrom(src => src.Pagos))
                // Vendedor y Caja
                .ForMember(dest => dest.VendedorId, opt => opt.MapFrom(src => src.VendedorId))
                .ForMember(dest => dest.VendedorNombre, opt => opt.MapFrom(src => src.Vendedor != null ? src.Vendedor.Nombre : null))
                .ForMember(dest => dest.VendedorCodigo, opt => opt.MapFrom(src => src.Vendedor != null ? src.Vendedor.Codigo : null))
                .ForMember(dest => dest.CajaId, opt => opt.MapFrom(src => src.CajaId))
                .ForMember(dest => dest.CajaCodigo, opt => opt.MapFrom(src => src.Caja != null ? src.Caja.Codigo : null));

            CreateMap<FacturaElectronica, FacturaListDto>()
                .ForMember(dest => dest.TipoDocumento, opt => opt.MapFrom(src => src.TipoDocumento.Valor))
                .ForMember(dest => dest.ReceptorNombre, opt => opt.MapFrom(src => src.Receptor != null ? src.Receptor.NombreRazonSocial : null))
                .ForMember(dest => dest.ReceptorNumeroDocumento, opt => opt.MapFrom(src => src.Receptor != null ? src.Receptor.NumeroDocumento : null));

            CreateMap<CreateFacturaDto, FacturaElectronica>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore()) // Se establece desde contexto
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.Receptor, opt => opt.Ignore())
                .ForMember(dest => dest.TipoDocumento, opt => opt.Ignore())
                // Campos generados automáticamente
                .ForMember(dest => dest.NumeroControl, opt => opt.Ignore())
                .ForMember(dest => dest.CodigoGeneracion, opt => opt.Ignore())
                // Totales calculados
                .ForMember(dest => dest.TotalNoSujeto, opt => opt.Ignore())
                .ForMember(dest => dest.TotalExento, opt => opt.Ignore())
                .ForMember(dest => dest.TotalGravado, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotalVentas, opt => opt.Ignore())
                .ForMember(dest => dest.DescuentoNoSujeto, opt => opt.Ignore())
                .ForMember(dest => dest.DescuentoExento, opt => opt.Ignore())
                .ForMember(dest => dest.DescuentoGravado, opt => opt.Ignore())
                .ForMember(dest => dest.TotalDescuento, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.IvaPercibido, opt => opt.Ignore())
                .ForMember(dest => dest.IvaRetenido, opt => opt.Ignore())
                .ForMember(dest => dest.RetencionRenta, opt => opt.Ignore())
                .ForMember(dest => dest.MontoTotalOperacion, opt => opt.Ignore())
                .ForMember(dest => dest.TotalNoGravado, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPagar, opt => opt.Ignore())
                .ForMember(dest => dest.TotalLetras, opt => opt.Ignore())
                .ForMember(dest => dest.SaldoFavor, opt => opt.Ignore())
                // Estado
                .ForMember(dest => dest.EstadoHacienda, opt => opt.Ignore())
                .ForMember(dest => dest.SelloRecibido, opt => opt.Ignore())
                // Listas - se mapean automáticamente
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles))
                .ForMember(dest => dest.Pagos, opt => opt.MapFrom(src => src.Pagos))
                .ForMember(dest => dest.Tributos, opt => opt.Ignore()) // Se calculan en el servicio
                .ForMember(dest => dest.DocumentosRelacionados, opt => opt.Ignore())
                .ForMember(dest => dest.Apendices, opt => opt.Ignore())
                .ForMember(dest => dest.Extension, opt => opt.Ignore());

            CreateMap<UpdateFacturaDto, FacturaElectronica>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore())
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.Receptor, opt => opt.Ignore())
                .ForMember(dest => dest.TipoDocumento, opt => opt.Ignore())
                .ForMember(dest => dest.NumeroControl, opt => opt.Ignore())
                .ForMember(dest => dest.CodigoGeneracion, opt => opt.Ignore())
                // Totales (se recalculan)
                .ForMember(dest => dest.TotalNoSujeto, opt => opt.Ignore())
                .ForMember(dest => dest.TotalExento, opt => opt.Ignore())
                .ForMember(dest => dest.TotalGravado, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotalVentas, opt => opt.Ignore())
                .ForMember(dest => dest.DescuentoNoSujeto, opt => opt.Ignore())
                .ForMember(dest => dest.DescuentoExento, opt => opt.Ignore())
                .ForMember(dest => dest.DescuentoGravado, opt => opt.Ignore())
                .ForMember(dest => dest.TotalDescuento, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.IvaPercibido, opt => opt.Ignore())
                .ForMember(dest => dest.IvaRetenido, opt => opt.Ignore())
                .ForMember(dest => dest.RetencionRenta, opt => opt.Ignore())
                .ForMember(dest => dest.MontoTotalOperacion, opt => opt.Ignore())
                .ForMember(dest => dest.TotalNoGravado, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPagar, opt => opt.Ignore())
                .ForMember(dest => dest.TotalLetras, opt => opt.Ignore())
                .ForMember(dest => dest.SaldoFavor, opt => opt.Ignore())
                .ForMember(dest => dest.EstadoHacienda, opt => opt.Ignore())
                .ForMember(dest => dest.SelloRecibido, opt => opt.Ignore())
                // Listas
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles))
                .ForMember(dest => dest.Pagos, opt => opt.MapFrom(src => src.Pagos))
                .ForMember(dest => dest.Tributos, opt => opt.Ignore())
                .ForMember(dest => dest.DocumentosRelacionados, opt => opt.Ignore())
                .ForMember(dest => dest.Apendices, opt => opt.Ignore())
                .ForMember(dest => dest.Extension, opt => opt.Ignore());

            // Mapeo de FacturaElectronica a FacturaElectronicaResponseDto
            // se define en FacturaProfile.cs con mapeos completos


            // ==========================================
            // PRODUCTOS/SERVICIOS
            // ==========================================
            CreateMap<ProductoServicio, ProductoServicioDto>()
                .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.UnidadMedida))
                .ForMember(dest => dest.TipoItem, opt => opt.MapFrom(src => src.TipoItem))
                .ForMember(dest => dest.EmisorNombre, opt => opt.MapFrom(src => src.Emisor != null ? src.Emisor.NombreRazonSocial : null))
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nombre : null))
                .ForMember(dest => dest.MarcaNombre, opt => opt.MapFrom(src => src.Marca != null ? src.Marca.Nombre : null))
                .ForMember(dest => dest.Sucursales, opt => opt.MapFrom(src =>
                    src.ProductoServicioSucursales != null
                        ? src.ProductoServicioSucursales.Select(ps => new SucursalAsignadaDto { Id = ps.SucursalId, Nombre = ps.Sucursal != null ? ps.Sucursal.Nombre : string.Empty }).ToList()
                        : new List<SucursalAsignadaDto>()))
                .ForMember(dest => dest.TributosAdicionales, opt => opt.MapFrom(src =>
                    src.TributosAdicionales != null
                        ? src.TributosAdicionales.Select(t => new ProductoTributoDto
                        {
                            Codigo = t.CatTributo != null ? t.CatTributo.Codigo : string.Empty,
                            TipoCalculo = t.TipoCalculo,
                            Valor = t.Valor
                        }).ToList()
                        : new List<ProductoTributoDto>()));

            CreateMap<ProductoServicio, ProductoServicioListDto>()
                .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.UnidadMedida.Valor))
                .ForMember(dest => dest.TipoItem, opt => opt.MapFrom(src => src.TipoItem.Valor))
                .ForMember(dest => dest.Sucursales, opt => opt.MapFrom(src =>
                    src.ProductoServicioSucursales != null
                        ? src.ProductoServicioSucursales.Select(ps => new SucursalAsignadaDto { Id = ps.SucursalId, Nombre = ps.Sucursal != null ? ps.Sucursal.Nombre : string.Empty }).ToList()
                        : new List<SucursalAsignadaDto>()))
                .ForMember(dest => dest.TributosAdicionales, opt => opt.MapFrom(src =>
                    src.TributosAdicionales != null
                        ? src.TributosAdicionales.Select(t => new ProductoTributoDto
                        {
                            Codigo = t.CatTributo != null ? t.CatTributo.Codigo : string.Empty,
                            TipoCalculo = t.TipoCalculo,
                            Valor = t.Valor
                        }).ToList()
                        : new List<ProductoTributoDto>()));

            CreateMap<CreateProductoServicioDto, ProductoServicio>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore()) // Se establece desde contexto
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.UnidadMedida, opt => opt.Ignore())
                .ForMember(dest => dest.TipoItem, opt => opt.Ignore())
                .ForMember(dest => dest.Categoria, opt => opt.Ignore())
                .ForMember(dest => dest.Marca, opt => opt.Ignore())
                .ForMember(dest => dest.AccesoTodasSucursales, opt => opt.Ignore())
                .ForMember(dest => dest.ProductoServicioSucursales, opt => opt.Ignore())
                .ForMember(dest => dest.Stocks, opt => opt.Ignore())
                .ForMember(dest => dest.Movimientos, opt => opt.Ignore())
                .ForMember(dest => dest.TributosAdicionales, opt => opt.Ignore());

            CreateMap<UpdateProductoServicioDto, ProductoServicio>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore())
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.UnidadMedida, opt => opt.Ignore())
                .ForMember(dest => dest.TipoItem, opt => opt.Ignore())
                .ForMember(dest => dest.Categoria, opt => opt.Ignore())
                .ForMember(dest => dest.Marca, opt => opt.Ignore())
                .ForMember(dest => dest.AccesoTodasSucursales, opt => opt.Ignore())
                .ForMember(dest => dest.ProductoServicioSucursales, opt => opt.Ignore())
                .ForMember(dest => dest.Stocks, opt => opt.Ignore())
                .ForMember(dest => dest.Movimientos, opt => opt.Ignore())
                .ForMember(dest => dest.TributosAdicionales, opt => opt.Ignore());

            // ==========================================
            // SUCURSALES
            // ==========================================
            CreateMap<Sucursal, SucursalDto>()
                .ForMember(dest => dest.EmisorNombre, opt => opt.MapFrom(src => src.Emisor != null ? src.Emisor.NombreRazonSocial : null))
                .ForMember(dest => dest.TotalFacturas, opt => opt.MapFrom(src => src.Facturas.Count))
                .ForMember(dest => dest.DepartamentoNombre, opt => opt.MapFrom(src => src.Departamento.Valor))
                .ForMember(dest => dest.MunicipioNombre, opt => opt.MapFrom(src => src.Municipio.Valor))
                .ForMember(dest => dest.DistritoNombre, opt => opt.MapFrom(src => src.Distrito != null ? src.Distrito.Valor : null))
                .ForMember(dest => dest.TipoEstablecimientoNombre, opt => opt.MapFrom(src => src.TipoEstablecimiento.Valor));

            CreateMap<Sucursal, SucursalListDto>();

            CreateMap<CreateSucursalDto, Sucursal>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore()) // Se establece desde contexto
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.Facturas, opt => opt.Ignore())
                .ForMember(dest => dest.Departamento, opt => opt.Ignore())
                .ForMember(dest => dest.Municipio, opt => opt.Ignore())
                .ForMember(dest => dest.TipoEstablecimiento, opt => opt.Ignore());

            CreateMap<UpdateSucursalDto, Sucursal>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore())
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.Facturas, opt => opt.Ignore())
                .ForMember(dest => dest.Departamento, opt => opt.Ignore())
                .ForMember(dest => dest.Municipio, opt => opt.Ignore())
                .ForMember(dest => dest.TipoEstablecimiento, opt => opt.Ignore());

            // ==========================================
            // CATÁLOGO MONEDA
            // ==========================================
            CreateMap<CatMoneda, CatalogoDto>();

            // ==========================================
            // SUSCRIPCIONES
            // ==========================================
            CreateMap<Suscripcion, SuscripcionResponseDto>()
                .ForMember(dest => dest.EmisorNombre, opt => opt.MapFrom(src => src.Emisor != null ? src.Emisor.NombreRazonSocial : string.Empty))
                .ForMember(dest => dest.EmisorCorreo, opt => opt.MapFrom(src => src.Emisor != null ? src.Emisor.CorreoElectronico : string.Empty))
                .ForMember(dest => dest.DiasRestantes, opt => opt.Ignore()) // Se calcula en el servicio
                .ForMember(dest => dest.Estado, opt => opt.Ignore()); // Se calcula en el servicio

            CreateMap<CreateSuscripcionDto, Suscripcion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore())
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.FacturasSuscripcion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaProximoCobro, opt => opt.Ignore()) // Se calcula en el servicio
                .ForMember(dest => dest.PrecioProrrateado, opt => opt.Ignore()) // Se calcula en el servicio
                .ForMember(dest => dest.DiasGracia, opt => opt.Ignore()); // Default 3

            CreateMap<UpdateSuscripcionDto, Suscripcion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.EmisorId, opt => opt.Ignore())
                .ForMember(dest => dest.Emisor, opt => opt.Ignore())
                .ForMember(dest => dest.FacturasSuscripcion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaProximoCobro, opt => opt.Ignore())
                .ForMember(dest => dest.PrecioProrrateado, opt => opt.Ignore())
                .ForMember(dest => dest.DiasGracia, opt => opt.Ignore());

            CreateMap<ConfiguracionProveedor, ConfiguracionProveedorDto>().ReverseMap()
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore());

            CreateMap<FacturaSuscripcion, FacturaSuscripcionResponseDto>()
                .ForMember(dest => dest.EmisorNombre, opt => opt.MapFrom(src =>
                    src.Suscripcion != null && src.Suscripcion.Emisor != null
                        ? src.Suscripcion.Emisor.NombreRazonSocial : string.Empty));

        }
    }
}

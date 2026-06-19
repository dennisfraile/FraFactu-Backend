using AutoMapper;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Domain.Entities;

namespace FraFactu.Application.Mappings;

/// <summary>
/// Perfil de mapeo para entidades de compras y gastos
/// </summary>
public class ComprasMappingProfile : Profile
{
    public ComprasMappingProfile()
    {
        // Proveedor mappings
        CreateMap<Proveedor, ProveedorDto>()
            .ForMember(dest => dest.TotalCompras, opt => opt.Ignore())
            .ForMember(dest => dest.MontoTotalCompras, opt => opt.Ignore())
            .ForMember(dest => dest.UltimaCompra, opt => opt.Ignore());

        CreateMap<CrearProveedorDto, Proveedor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.FechaCreacion, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<ActualizarProveedorDto, Proveedor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NIT, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore());

        // CompraExterna mappings
        CreateMap<CompraExterna, CompraExternaDto>()
            .ForMember(dest => dest.ProveedorNIT, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.NIT : ""))
            .ForMember(dest => dest.ProveedorNombre, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.Nombre : ""))
            .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles))
            .ForMember(dest => dest.Gastos, opt => opt.MapFrom(src => src.Gastos))
            .ForMember(dest => dest.TotalProductosInventario, opt => opt.Ignore())
            .ForMember(dest => dest.TotalGastosAdministrativos, opt => opt.Ignore())
            .ForMember(dest => dest.CantidadItems, opt => opt.Ignore());

        CreateMap<CrearCompraExternaDto, CompraExterna>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FechaRegistro, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => "BORRADOR"))
            .ForMember(dest => dest.FechaConfirmacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaAnulacion, opt => opt.Ignore())
            .ForMember(dest => dest.Proveedor, opt => opt.Ignore())
            .ForMember(dest => dest.Detalles, opt => opt.Ignore())
            .ForMember(dest => dest.Gastos, opt => opt.Ignore());

        // CompraExternaDetalle mappings
        CreateMap<CompraExternaDetalle, CompraDetalleDto>()
            .ForMember(dest => dest.ProductoCodigo, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Codigo : ""))
            .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : ""))
            .ForMember(dest => dest.BodegaNombre, opt => opt.MapFrom(src => src.Bodega != null ? src.Bodega.Nombre : ""));

        CreateMap<CrearCompraDetalleDto, CompraExternaDetalle>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompraExternaId, opt => opt.Ignore())
            .ForMember(dest => dest.CompraExterna, opt => opt.Ignore())
            .ForMember(dest => dest.Producto, opt => opt.Ignore())
            .ForMember(dest => dest.Bodega, opt => opt.Ignore());

        // GastoAdministrativo mappings
        CreateMap<GastoAdministrativo, GastoAdministrativoDto>()
            .ForMember(dest => dest.TipoGastoNombre, opt => opt.MapFrom(src => src.TipoGasto != null ? src.TipoGasto.Nombre : null));

        CreateMap<CrearGastoDto, GastoAdministrativo>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompraExternaId, opt => opt.Ignore())
            .ForMember(dest => dest.CompraExterna, opt => opt.Ignore())
            .ForMember(dest => dest.TipoGasto, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}

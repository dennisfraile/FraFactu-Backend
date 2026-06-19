using AutoMapper;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Domain.Entities;

namespace FraFactu.Application.Mappings;

/// <summary>
/// Perfil de mapeo para DTEs recibidos
/// </summary>
public class DteRecibidoMappingProfile : Profile
{
    public DteRecibidoMappingProfile()
    {
        CreateMap<DteRecibido, DteRecibidoDto>();

        CreateMap<DteRecibido, DteRecibidoResumenDto>();
    }
}

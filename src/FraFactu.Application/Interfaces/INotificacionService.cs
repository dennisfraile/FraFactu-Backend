using FraFactu.Application.DTOs.Notificaciones;

namespace FraFactu.Application.Interfaces
{
    public interface INotificacionService
    {
        Task<NotificacionDto> CrearAsync(int emisorId, string tipo, string titulo, string mensaje,
            string? ruta = null, int? referenciaId = null, string nivel = "info");
        Task<List<NotificacionDto>> ListarAsync(int emisorId, int usuarioId, int max = 30);
        Task<int> ContarNoLeidasAsync(int emisorId, int usuarioId);
        Task MarcarLeidaAsync(int notificacionId, int usuarioId);
        Task MarcarTodasLeidasAsync(int emisorId, int usuarioId);
    }
}

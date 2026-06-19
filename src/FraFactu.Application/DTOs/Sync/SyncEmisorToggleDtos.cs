namespace FraFactu.Application.DTOs.Sync;

/// <summary>
/// SmartHub avisa cuando se (des)activa un Hub para que el Emisor 1:1 quede
/// alineado en Smartix. Idempotente.
/// </summary>
public class SyncEmisorToggleRequestDto
{
    public int EmisorId { get; set; }
    public bool Activo { get; set; }
}

public class SyncEmisorToggleResponseDto
{
    public bool Activo { get; set; }
    public bool Encontrado { get; set; }
    public bool Cambio { get; set; }
    public int? Id { get; set; }
}

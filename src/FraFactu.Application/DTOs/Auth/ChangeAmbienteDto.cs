namespace FraFactu.Application.DTOs.Auth;

/// <summary>
/// Petición para cambiar el ambiente (Pruebas/Producción) del Emisor asociado
/// al usuario autenticado.
/// </summary>
public class ChangeAmbienteDto
{
    /// <summary>Código MH del ambiente: "00" = Pruebas, "01" = Producción.</summary>
    public string Ambiente { get; set; } = "00";
}

namespace FraFactu.Domain.Enums;

/// <summary>
/// Estado del ciclo de vida de un DTE recibido.
/// Los miembros se nombran en MAYÚSCULAS a propósito: con
/// <c>HasConversion&lt;string&gt;()</c> EF Core persiste el nombre del miembro
/// y los datos ya existentes en BD usan "PENDIENTE", "VINCULADO" y
/// "DESCARTADO". Mantenerlos así evita una migración de datos sobre la base
/// remota y conserva el contrato de la API (el frontend filtra y pinta los
/// chips de estado con estos mismos literales).
/// </summary>
public enum EstadoDteRecibido
{
    PENDIENTE,
    VINCULADO,
    DESCARTADO
}

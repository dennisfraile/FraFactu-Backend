namespace FraFactu.Domain.Enums;

/// <summary>
/// F4 (Plan inventario desde DTE): estados de una divergencia detectada por
/// el job de reconciliacion que compara Smartix.StockBodega contra
/// SmartInventory.StockBodega. <c>DETECTADA</c> es el estado abierto;
/// <c>RESUELTA</c> cuando una corrida posterior confirma que los stocks
/// volvieron a coincidir.
/// </summary>
public enum EstadoDivergenciaInventario
{
    DETECTADA = 0,
    RESUELTA = 1
}

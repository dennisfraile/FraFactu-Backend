namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO de respuesta con información de saldo de un DTE para Notas de Crédito
    /// </summary>
    public class SaldoDteDto
    {
        public int Id { get; set; }
        public string DteCodigoGeneracion { get; set; } = string.Empty;
        public string TipoDte { get; set; } = string.Empty;
        public decimal MontoOriginal { get; set; }
        public decimal MontoAcreditado { get; set; }
        public decimal SaldoDisponible { get; set; }
        public int NceCount { get; set; }
    }
}

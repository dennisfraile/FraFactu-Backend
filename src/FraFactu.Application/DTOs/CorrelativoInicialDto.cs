namespace FraFactu.Application.DTOs
{
    public class CorrelativoInicialDto
    {
        public int Id { get; set; }
        public string TipoDte { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string Ambiente { get; set; } = string.Empty;
        public int UltimoCorrelativo { get; set; }
        /// <summary>True si ya hay DTE emitidos para esta combinación (no editable).</summary>
        public bool Bloqueado { get; set; }
    }

    /// <summary>Upsert de correlativo inicial; se identifica por (EmisorId del token, TipoDte, Anio, Ambiente).</summary>
    public class CorrelativoInicialUpsertDto
    {
        public string TipoDte { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string Ambiente { get; set; } = string.Empty;
        public int UltimoCorrelativo { get; set; }
    }
}

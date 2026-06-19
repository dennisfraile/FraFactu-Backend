namespace FraFactu.Application.DTOs.Import;

public class ImportResultDto
{
    public int TotalFilas { get; set; }
    public int Exitosos { get; set; }
    public int Fallidos { get; set; }
    public int CategoriasCreadas { get; set; }
    public int MarcasCreadas { get; set; }
    public List<ImportErrorDto> Errores { get; set; } = new();
}

public class ImportErrorDto
{
    public int Fila { get; set; }
    public string Campo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

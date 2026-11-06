namespace ProjectSaborLatino.DTOs.Sistema;

public class ResultadoSubidaArchivoDto
{
    public string Url { get; set; } = string.Empty;
    public string RutaRelativa { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
}

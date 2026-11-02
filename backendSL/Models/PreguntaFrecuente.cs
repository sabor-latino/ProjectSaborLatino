using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.Models;

// Preguntas y respuestas que se muestran en la página pública
public class PreguntaFrecuente
{
    public int Id { get; set; }

    [MaxLength(300)]
    public string Pregunta { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? PreguntaEn { get; set; }

    [MaxLength(2000)]
    public string Respuesta { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? RespuestaEn { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}

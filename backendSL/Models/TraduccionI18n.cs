using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProjectSaborLatino.Models;

// Almacena traducciones dinámicas automáticas o manuales de cadenas de texto
// generadas en backend para la arquitectura i18n multilingüe (en, fr, pt).
[Index(nameof(HashOrigen), nameof(IdiomaDestino), IsUnique = true)]
public class TraduccionI18n
{
    public int Id { get; set; }

    // Hash SHA256 del texto de origen para búsqueda O(1) indexada
    [MaxLength(64)]
    public string HashOrigen { get; set; } = string.Empty;

    // Texto original en español (clave principal en la arquitectura i18n)
    [MaxLength(2000)]
    public string TextoOrigen { get; set; } = string.Empty;

    // Código de idioma destino ("en", "fr", "pt")
    [MaxLength(10)]
    public string IdiomaDestino { get; set; } = string.Empty;

    // Texto traducido correspondiente
    [MaxLength(2500)]
    public string TextoTraducido { get; set; } = string.Empty;

    // Indica si fue generada por el motor automático o ajustada manualmente
    public bool EsAutomatica { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

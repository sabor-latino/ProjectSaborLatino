using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Catalogo;

// Lo que el API devuelve
public class ImagenGaleriaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? TituloEn { get; set; }
    public string ImagenUrl { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; }
}

// Lo que el API recibe al crear o editar
public class ImagenGaleriaGuardarDto
{
    [Required, MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? TituloEn { get; set; }

    [Required, MaxLength(500), UrlHttpAbsoluta]
    public string ImagenUrl { get; set; } = string.Empty;

    [Range(0, 1000)]
    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}
// Acepta solo URLs absolutas http/https (rechaza javascript:, data:, rutas relativas, etc.)
[AttributeUsage(AttributeTargets.Property)]
public sealed class UrlHttpAbsolutaAttribute : ValidationAttribute
{
    public UrlHttpAbsolutaAttribute()
        : base("La URL de la imagen debe ser una dirección absoluta http o https válida.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return true;
            return Uri.TryCreate(s, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && !string.IsNullOrEmpty(uri.Host);
        }
        return false;
    }
}

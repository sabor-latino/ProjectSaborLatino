using ProjectSaborLatino.DTOs.Catalogo;
using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Sistema;

// Contenido editable de la página de inicio (bilingüe ES/EN)
public class ContenidoInicioDto
{
    [Required, MaxLength(120)]
    public string EtiquetaPortada { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string EtiquetaPortadaEn { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string TituloPortada { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string TituloPortadaEn { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string SubtituloPortada { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string SubtituloPortadaEn { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Paso1Titulo { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Paso1TituloEn { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Paso1Texto { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Paso1TextoEn { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Paso2Titulo { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Paso2TituloEn { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Paso2Texto { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Paso2TextoEn { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Paso3Titulo { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Paso3TituloEn { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Paso3Texto { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Paso3TextoEn { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CtaTitulo { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CtaTituloEn { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string CtaSubtitulo { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string CtaSubtituloEn { get; set; } = string.Empty;

    [MaxLength(500), UrlHttpAbsoluta]
    public string? FotoPortadaUrl { get; set; }

    [MaxLength(500), UrlHttpAbsoluta]
    public string? FotoCotizarUrl { get; set; }
}

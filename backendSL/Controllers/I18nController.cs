using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

[ApiController]
[Route("api/i18n")]
public class I18nController : ControllerBase
{
    private readonly ITraduccionI18nService _traduccionService;

    public I18nController(ITraduccionI18nService traduccionService)
    {
        _traduccionService = traduccionService;
    }

    // GET api/i18n/catalogo → Entrega el catálogo dinámico completo en estructura i18n Record<Idioma, Record<string, string>>
    [HttpGet("catalogo")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, Dictionary<string, string>>>> ObtenerCatalogoDinamico()
    {
        var catalogo = await _traduccionService.ObtenerCatalogoDinamicoAsync();
        return Ok(catalogo);
    }

    // POST api/i18n/traducir → Traduce una o varias cadenas a un idioma destino
    [HttpPost("traducir")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, string>>> TraducirLote([FromBody] PeticionTraducirDto dto)
    {
        if (dto.Textos is null || dto.Textos.Count == 0 || string.IsNullOrWhiteSpace(dto.IdiomaDestino))
        {
            return Ok(new Dictionary<string, string>());
        }

        var resultado = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var texto in dto.Textos.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.Ordinal))
        {
            resultado[texto] = await _traduccionService.TraducirAsync(texto, dto.IdiomaDestino);
        }

        return Ok(resultado);
    }
}

public class PeticionTraducirDto
{
    public List<string> Textos { get; set; } = [];
    public string IdiomaDestino { get; set; } = string.Empty;
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public class TraduccionI18nService : ITraduccionI18nService
{
    private static readonly string[] IdiomasDestinoActivos = ["en", "fr", "pt"];
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TraduccionI18nService> _logger;

    public TraduccionI18nService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<TraduccionI18nService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> TraducirAsync(string textoOrigen, string idiomaDestino, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(textoOrigen))
        {
            return string.Empty;
        }

        var idioma = idiomaDestino.Trim().ToLowerInvariant();
        if (idioma == "es")
        {
            return textoOrigen;
        }

        var hash = CalcularHash(textoOrigen);

        // 1. Verificar si ya existe en la base de datos (0 ms)
        var existente = await _db.TraduccionesI18n
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.HashOrigen == hash && t.IdiomaDestino == idioma, cancellationToken);

        if (existente is not null && !string.IsNullOrWhiteSpace(existente.TextoTraducido))
        {
            return existente.TextoTraducido;
        }

        // 2. Invocar el motor de traducción externa
        var traducido = await EjecutarTraduccionExternaAsync(textoOrigen, idioma, cancellationToken);

        // 3. Persistir en base de datos para no volver a consultar la red
        try
        {
            var nuevaTraduccion = new TraduccionI18n
            {
                HashOrigen = hash,
                TextoOrigen = textoOrigen.Trim(),
                IdiomaDestino = idioma,
                TextoTraducido = traducido,
                EsAutomatica = true,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            };

            _db.TraduccionesI18n.Add(nuevaTraduccion);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Manejo de concurrencia en caso de que otra petición haya guardado la misma clave simultáneamente
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo guardar en base de datos la traducción para '{TextoOrigen}' al idioma '{Idioma}'.", textoOrigen, idioma);
        }

        return traducido;
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> ObtenerCatalogoDinamicoAsync(CancellationToken cancellationToken = default)
    {
        var todas = await _db.TraduccionesI18n
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var resultado = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new(StringComparer.Ordinal),
            ["fr"] = new(StringComparer.Ordinal),
            ["pt"] = new(StringComparer.Ordinal)
        };

        foreach (var item in todas)
        {
            var lang = item.IdiomaDestino.ToLowerInvariant();
            if (!resultado.ContainsKey(lang))
            {
                resultado[lang] = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            resultado[lang][item.TextoOrigen] = item.TextoTraducido;
        }

        return resultado;
    }

    public async Task SincronizarTraduccionesAsync(IEnumerable<string?> textosOrigen, CancellationToken cancellationToken = default)
    {
        var textosUnicos = textosOrigen
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (textosUnicos.Count == 0)
        {
            return;
        }

        foreach (var texto in textosUnicos)
        {
            foreach (var lang in IdiomasDestinoActivos)
            {
                try
                {
                    await TraducirAsync(texto, lang, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al sincronizar traducción para '{Texto}' a '{Idioma}'.", texto, lang);
                }
            }
        }
    }

    public async Task<string?> TraducirAInglesSiVacioAsync(string? textoEnActual, string? textoEs, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(textoEnActual))
        {
            return textoEnActual.Trim();
        }

        if (string.IsNullOrWhiteSpace(textoEs))
        {
            return textoEnActual;
        }

        return await TraducirAsync(textoEs.Trim(), "en", cancellationToken);
    }

    private async Task<string> EjecutarTraduccionExternaAsync(string textoOrigen, string idiomaDestino, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            // Google GTX requiere User-Agent de navegador para evitar HTTP 403
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=es&tl={Uri.EscapeDataString(idiomaDestino)}&dt=t&q={Uri.EscapeDataString(textoOrigen)}");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Respuesta no exitosa del motor de traducción ({StatusCode}). Retornando texto original.", response.StatusCode);
                return textoOrigen;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var primerNivel = root[0];
                if (primerNivel.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var item in primerNivel.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
                        {
                            var parte = item[0].GetString();
                            if (!string.IsNullOrEmpty(parte))
                            {
                                sb.Append(parte);
                            }
                        }
                    }

                    var final = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(final))
                    {
                        return final;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo al contactar motor de traducción para '{TextoOrigen}'. Retornando texto original de forma segura.", textoOrigen);
        }

        return textoOrigen;
    }

    private static string CalcularHash(string texto)
    {
        var bytes = Encoding.UTF8.GetBytes(texto.Trim());
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}

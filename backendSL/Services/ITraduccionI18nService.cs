namespace ProjectSaborLatino.Services;

// Servicio centralizado de traducción dinámica para la arquitectura multilingüe i18n
public interface ITraduccionI18nService
{
    // Obtiene una traducción desde la base de datos o la traduce en caliente y la almacena
    Task<string> TraducirAsync(string textoOrigen, string idiomaDestino, CancellationToken cancellationToken = default);

    // Obtiene el catálogo completo de traducciones dinámicas agrupadas por idioma destino ("en", "fr", "pt")
    // Formato exacto i18n: Record<Idioma, Record<string, string>> (ej: { "en": { "Luces": "Lights" } })
    Task<Dictionary<string, Dictionary<string, string>>> ObtenerCatalogoDinamicoAsync(CancellationToken cancellationToken = default);

    // Sincroniza un conjunto de textos en español: traduce a los idiomas activos (en, fr, pt) si no están en BD
    Task SincronizarTraduccionesAsync(IEnumerable<string?> textosOrigen, CancellationToken cancellationToken = default);

    // Traduce un texto a inglés inmediatamente si el campo en inglés viene vacío
    Task<string?> TraducirAInglesSiVacioAsync(string? textoEnActual, string? textoEs, CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Http;
using ProjectSaborLatino.DTOs.Sistema;

namespace ProjectSaborLatino.Services;

public class ArchivoStorageService : IArchivoStorageService
{
    private const long MaxTamanoBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly string _rutaBaseUploads;

    public ArchivoStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _rutaBaseUploads = configuration["Almacenamiento:RutaUploads"]
            ?? Path.Combine(environment.ContentRootPath, "uploads");

        if (!Directory.Exists(_rutaBaseUploads))
        {
            Directory.CreateDirectory(_rutaBaseUploads);
        }
    }

    public async Task<ResultadoSubidaArchivoDto> GuardarImagenAsync(IFormFile archivo, HttpRequest request, string subcarpeta = "imagenes")
    {
        if (archivo == null || archivo.Length == 0)
        {
            throw new ArgumentException("No se seleccionó ningún archivo para subir.");
        }

        if (archivo.Length > MaxTamanoBytes)
        {
            throw new ArgumentException("El archivo excede el tamaño máximo permitido de 10 MB.");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !ExtensionesPermitidas.Contains(extension))
        {
            throw new ArgumentException("Formato no permitido. Solo se aceptan imágenes JPG, PNG o WebP.");
        }

        // Validación de magic bytes para evitar camuflaje de ejecutables
        using (var streamValidacion = archivo.OpenReadStream())
        {
            if (!EsFirmaImagenValida(streamValidacion, extension))
            {
                throw new ArgumentException("El contenido del archivo no corresponde a una imagen válida.");
            }
        }

        var extensionNormalizada = extension == ".jpeg" ? ".jpg" : extension;
        var nombreArchivo = $"{Guid.NewGuid():N}{extensionNormalizada}";

        var carpetaDestino = Path.Combine(_rutaBaseUploads, subcarpeta);
        if (!Directory.Exists(carpetaDestino))
        {
            Directory.CreateDirectory(carpetaDestino);
        }

        var rutaFisicaCompleta = Path.Combine(carpetaDestino, nombreArchivo);
        using (var streamDestino = new FileStream(rutaFisicaCompleta, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await archivo.CopyToAsync(streamDestino);
        }

        var scheme = request.Scheme;
        var host = request.Host.Value;
        var rutaRelativa = $"/uploads/{subcarpeta}/{nombreArchivo}";
        var urlAbsoluta = $"{scheme}://{host}{rutaRelativa}";

        return new ResultadoSubidaArchivoDto
        {
            Url = urlAbsoluta,
            RutaRelativa = rutaRelativa,
            NombreOriginal = Path.GetFileName(archivo.FileName),
            TamanoBytes = archivo.Length
        };
    }

    private static bool EsFirmaImagenValida(Stream stream, string extension)
    {
        Span<byte> buffer = stackalloc byte[12];
        var leidos = stream.Read(buffer);
        if (leidos < 4) return false;

        return extension switch
        {
            ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".png" => leidos >= 8 &&
                      buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                      buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A,
            ".webp" => leidos >= 12 &&
                       buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 && // RIFF
                       buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50, // WEBP
            _ => false
        };
    }
}

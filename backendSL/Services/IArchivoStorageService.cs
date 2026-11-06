using Microsoft.AspNetCore.Http;
using ProjectSaborLatino.DTOs.Sistema;

namespace ProjectSaborLatino.Services;

public interface IArchivoStorageService
{
    Task<ResultadoSubidaArchivoDto> GuardarImagenAsync(IFormFile archivo, HttpRequest request, string subcarpeta = "imagenes");
}

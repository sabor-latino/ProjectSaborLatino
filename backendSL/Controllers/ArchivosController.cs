using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

[ApiController]
[Route("api/archivos")]
[Authorize(Roles = $"{Roles.Superadministrador},{Roles.Administrador}")]
public class ArchivosController : ControllerBase
{
    private readonly IArchivoStorageService _storageService;
    private readonly IBitacoraService _bitacora;

    public ArchivosController(IArchivoStorageService storageService, IBitacoraService bitacora)
    {
        _storageService = storageService;
        _bitacora = bitacora;
    }

    [HttpPost("subir-imagen")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB límite de solicitud
    public async Task<ActionResult<ResultadoSubidaArchivoDto>> SubirImagen(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest(new { mensaje = "No se envió ningún archivo de imagen para subir." });
        }

        try
        {
            var resultado = await _storageService.GuardarImagenAsync(archivo, Request, "imagenes");
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _bitacora.RegistrarAsync(usuarioId, "SubirImagen", "Archivo", resultado.RutaRelativa, $"{resultado.NombreOriginal} ({resultado.TamanoBytes} bytes)");

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Sistema;

// Lo que el API devuelve de cada usuario
public class UsuarioDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
}

// Lo que manda el admin para crear un usuario
public class UsuarioCrearDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Phone, MaxLength(30)]
    public string? Telefono { get; set; }

    // "Superadministrador", "Administrador", "Trabajador" o "Cliente"
    [Required]
    public string Rol { get; set; } = string.Empty;
}

// PUT api/usuarios/{id}/perfil o PUT api/usuarios/{id}
public class UsuarioModificarPerfilDto
{
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Phone, MaxLength(30)]
    public string? Telefono { get; set; }
}

// PATCH api/usuarios/{id}/rol
public class UsuarioCambiarRolDto
{
    [Required]
    public string Rol { get; set; } = string.Empty;
}

// PATCH api/usuarios/{id}/activo
public class UsuarioCambiarActivoDto
{
    [Required]
    public bool? Activo { get; set; }
}

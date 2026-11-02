using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Catalogo;

// Lo que el API devuelve
public class PaqueteDto
{
    public int Id { get; set; }
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = string.Empty;
    public string? ServicioNombreEn { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? NombreEn { get; set; }
    public string? Descripcion { get; set; }
    public string? DescripcionEn { get; set; }
    public decimal PrecioBase { get; set; }
    public string? ImagenUrl { get; set; }
    public string? PotenciaSonido { get; set; }
    public string? CoberturaAforo { get; set; }
    public string? DuracionMontaje { get; set; }
    public string? RequerimientosElectricos { get; set; }
    public string? EspacioMinimo { get; set; }
    public string? FichaTecnica { get; set; }
    public string? FichaTecnicaEn { get; set; }
    public bool Activo { get; set; }
    public List<PaqueteEquipoDto> Equipos { get; set; } = [];
}

// Un equipo dentro del paquete, ya con su nombre para mostrarlo
public class PaqueteEquipoDto
{
    public int EquipoId { get; set; }
    public string EquipoNombre { get; set; } = string.Empty;
    public string? EquipoNombreEn { get; set; }
    public string? EspecificacionTecnica { get; set; }
    public string? EspecificacionTecnicaEn { get; set; }
    public int Cantidad { get; set; }
}

// Lo que el API recibe al crear o editar
public class PaqueteGuardarDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un servicio válido.")]
    public int ServicioId { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NombreEn { get; set; }

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [MaxLength(1000)]
    public string? DescripcionEn { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "El precio no puede ser negativo.")]
    public decimal PrecioBase { get; set; }

    [MaxLength(500), UrlHttpAbsoluta]
    public string? ImagenUrl { get; set; }

    [MaxLength(100)]
    public string? PotenciaSonido { get; set; }

    [MaxLength(100)]
    public string? CoberturaAforo { get; set; }

    [MaxLength(150)]
    public string? DuracionMontaje { get; set; }

    [MaxLength(250)]
    public string? RequerimientosElectricos { get; set; }

    [MaxLength(150)]
    public string? EspacioMinimo { get; set; }

    [MaxLength(1500)]
    public string? FichaTecnica { get; set; }

    [MaxLength(1500)]
    public string? FichaTecnicaEn { get; set; }

    public bool Activo { get; set; } = true;

    // Equipo que incluye (puede venir vacío)
    public List<PaqueteEquipoGuardarDto> Equipos { get; set; } = [];
}

public class PaqueteEquipoGuardarDto
{
    [Range(1, int.MaxValue)]
    public int EquipoId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad debe ser al menos 1.")]
    public int Cantidad { get; set; } = 1;
}

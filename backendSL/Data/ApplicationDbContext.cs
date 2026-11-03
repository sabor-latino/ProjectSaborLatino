using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // =============== Catálogo (Fase 1) ===============
    // Cada DbSet se convierte en una tabla de la base de datos

    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Paquete> Paquetes => Set<Paquete>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<PaqueteEquipo> PaquetesEquipos => Set<PaqueteEquipo>();
    public DbSet<ImagenGaleria> ImagenesGaleria => Set<ImagenGaleria>();
    public DbSet<PreguntaFrecuente> PreguntasFrecuentes => Set<PreguntaFrecuente>();
    public DbSet<PromocionTemporada> PromocionesTemporada => Set<PromocionTemporada>();
    public DbSet<PromocionTemporadaPaquete> PromocionesTemporadaPaquetes => Set<PromocionTemporadaPaquete>();

    // =============== Solicitudes y cotizaciones (Fase 2) ===============

    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();

    // =============== Eventos (Fase 3) ===============

    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<EventoEquipo> EventosEquipos => Set<EventoEquipo>();
    public DbSet<EventoTrabajador> EventosTrabajadores => Set<EventoTrabajador>();
    public DbSet<EventoHistorialEstado> EventosHistorialEstados => Set<EventoHistorialEstado>();

    // =============== Usuarios y sistema (Fase 4) ===============

    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
    public DbSet<Bitacora> Bitacora => Set<Bitacora>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity necesita configurar sus propias tablas primero
        base.OnModelCreating(builder);

        builder.Entity<PromocionTemporadaPaquete>(e =>
        {
            e.HasIndex(x => new { x.PromocionTemporadaId, x.PaqueteId }).IsUnique();

            e.HasOne(x => x.PromocionTemporada)
                .WithMany(p => p.Paquetes)
                .HasForeignKey(x => x.PromocionTemporadaId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Paquete)
                .WithMany()
                .HasForeignKey(x => x.PaqueteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Solicitud>(e =>
        {
            e.HasIndex(s => s.ClaveIdempotencia).IsUnique()
                .HasFilter("[ClaveIdempotencia] IS NOT NULL");

            // El estado se guarda como texto ("Pendiente") y no como número (0)
            e.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20);

            // Si se borra el usuario, la solicitud queda sin dueño pero no se pierde
            e.HasOne(s => s.ClienteUsuario)
                .WithMany()
                .HasForeignKey(s => s.ClienteUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            // No se puede borrar un paquete que tiene solicitudes
            e.HasOne(s => s.Paquete)
                .WithMany()
                .HasForeignKey(s => s.PaqueteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Cotizacion>(e =>
        {
            e.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);

            // Si se borra la solicitud, se borran sus cotizaciones
            e.HasOne(c => c.Solicitud)
                .WithMany(s => s.Cotizaciones)
                .HasForeignKey(c => c.SolicitudId)
                .OnDelete(DeleteBehavior.Cascade);

            // No se puede borrar un usuario que creó cotizaciones
            e.HasOne(c => c.CreadaPorUsuario)
                .WithMany()
                .HasForeignKey(c => c.CreadaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Empleado>(e =>
        {
            // No se repite la cédula
            e.HasIndex(x => x.Cedula).IsUnique();

            // Una cuenta de usuario solo puede estar enlazada a un empleado (si tiene)
            e.HasIndex(x => x.UsuarioId).IsUnique().HasFilter("[UsuarioId] IS NOT NULL");

            // Si se borra el usuario, el empleado se queda sin cuenta
            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Evento>(e =>
        {
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);

            // Una cotización solo puede generar un evento
            e.HasIndex(x => x.CotizacionId).IsUnique();

            e.HasOne(x => x.Cotizacion)
                .WithMany()
                .HasForeignKey(x => x.CotizacionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Paquete)
                .WithMany()
                .HasForeignKey(x => x.PaqueteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EventoEquipo>(e =>
        {
            // Si se borra el evento, se borra su reserva de equipo
            e.HasOne(x => x.Evento)
                .WithMany(ev => ev.Equipos)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Cascade);

            // No se puede borrar un equipo reservado en algún evento
            e.HasOne(x => x.Equipo)
                .WithMany()
                .HasForeignKey(x => x.EquipoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EventoTrabajador>(e =>
        {
            e.HasOne(x => x.Evento)
                .WithMany(ev => ev.Trabajadores)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EventoHistorialEstado>(e =>
        {
            e.Property(x => x.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EstadoNuevo).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.Evento)
                .WithMany(ev => ev.Historial)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // La bitácora se consulta casi siempre por fecha
        builder.Entity<Bitacora>().HasIndex(x => x.Fecha);
    }
}

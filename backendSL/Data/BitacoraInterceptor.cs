using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Data;

// Escribe en la Bitacora cada registro del negocio que se crea, modifica o elimina.
// Funciona en dos pasos: antes de guardar anota los cambios; después de guardar
// (cuando ya existen los Id nuevos) escribe las filas de la bitácora.
public class BitacoraInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Cambios anotados antes de guardar, pendientes de escribir
    private readonly List<CambioPendiente> _pendientes = [];
    private IDbContextTransaction? _transaction;
    private IDbContextTransaction? _externalTransaction;
    private bool _writingAudit;

    public BitacoraInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (!_writingAudit)
        {
            Anotar(eventData.Context);
            if (_pendientes.Count > 0)
            {
                _externalTransaction = eventData.Context!.Database.CurrentTransaction;
                if (_externalTransaction is null) _transaction = eventData.Context.Database.BeginTransaction();
            }
        }
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_writingAudit)
        {
            Anotar(eventData.Context);
            if (_pendientes.Count > 0)
            {
                _externalTransaction = eventData.Context!.Database.CurrentTransaction;
                if (_externalTransaction is null) _transaction = await eventData.Context.Database.BeginTransactionAsync(cancellationToken);
            }
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_pendientes.Count > 0)
        {
            try
            {
                EscribirPendientes(eventData.Context);
                _writingAudit = true;
                eventData.Context!.SaveChanges();
                _transaction?.Commit();
            }
            catch
            {
                _transaction?.Rollback();
                // Con MARS activo no hay savepoints fiables: se revierte toda
                // la transacción ajena para impedir que el llamador confirme negocio sin auditoría.
                _externalTransaction?.Rollback();
                throw;
            }
            finally
            {
                _writingAudit = false;
                _transaction?.Dispose();
                _transaction = null;
                _externalTransaction = null;
                _pendientes.Clear();
            }
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_pendientes.Count > 0)
        {
            try
            {
                EscribirPendientes(eventData.Context);
                _writingAudit = true;
                await eventData.Context!.SaveChangesAsync(cancellationToken);
                if (_transaction is not null) await _transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                if (_transaction is not null) await _transaction.RollbackAsync();
                if (_externalTransaction is not null) await _externalTransaction.RollbackAsync();
                throw;
            }
            finally
            {
                _writingAudit = false;
                if (_transaction is not null) await _transaction.DisposeAsync();
                _transaction = null;
                _externalTransaction = null;
                _pendientes.Clear();
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // Si el guardado falla, se descartan los cambios anotados
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendientes.Clear();
        if (!_writingAudit)
        {
            _transaction?.Rollback();
            _externalTransaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
            _externalTransaction = null;
        }
        base.SaveChangesFailed(eventData);
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendientes.Clear();
        if (!_writingAudit)
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }
            if (_externalTransaction is not null) await _externalTransaction.RollbackAsync();
            _transaction = null;
            _externalTransaction = null;
        }
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Anotar(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)
                || !SeAudita(entry))
            {
                continue;
            }

            _pendientes.Add(new CambioPendiente
            {
                Entry = entry,
                Accion = entry.State switch
                {
                    EntityState.Added => "Crear",
                    EntityState.Modified => "Modificar",
                    _ => "Eliminar"
                },
                // Los Id de registros nuevos todavía no existen; se leen después de guardar
                EntidadId = entry.State == EntityState.Added ? null : LeerClave(entry),
                Detalle = entry.State == EntityState.Modified ? PropiedadesModificadas(entry) : null
            });
        }
    }

    // Devuelve true si agregó filas a la bitácora (y entonces hay que guardar otra vez)
    private bool EscribirPendientes(DbContext? context)
    {
        if (context is null || _pendientes.Count == 0)
        {
            return false;
        }

        var usuarioId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var cambio in _pendientes)
        {
            context.Set<Bitacora>().Add(new Bitacora
            {
                UsuarioId = usuarioId,
                Accion = cambio.Accion,
                Entidad = cambio.Entry.Metadata.ClrType.Name,
                EntidadId = cambio.EntidadId ?? LeerClave(cambio.Entry),
                Detalle = cambio.Detalle
            });
        }

        // Se vacía antes de guardar: ese segundo guardado solo trae filas de Bitacora,
        // que no se auditan, así que no se repite el ciclo
        _pendientes.Clear();
        return true;
    }

    // Solo las tablas del negocio: ni la propia Bitacora ni las tablas de Identity (usuarios, roles...)
    private static bool SeAudita(EntityEntry entry)
    {
        var tipo = entry.Metadata.ClrType;

        return tipo != typeof(Bitacora)
            && tipo != typeof(ApplicationUser)
            && tipo.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") != true;
    }

    // "5" para claves simples, "3,7" para claves dobles
    private static string LeerClave(EntityEntry entry)
    {
        var clave = entry.Metadata.FindPrimaryKey()!;
        return string.Join(",", clave.Properties.Select(p => entry.Property(p.Name).CurrentValue));
    }

    // "PrecioBase: 120000 → 130000; Nombre: A → B"
    private static string? PropiedadesModificadas(EntityEntry entry)
    {
        var cambios = entry.Properties
            .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
            .Select(p => $"{p.Metadata.Name}: {p.OriginalValue ?? "null"} → {p.CurrentValue ?? "null"}")
            .ToList();

        if (cambios.Count == 0)
        {
            return null;
        }

        var texto = string.Join("; ", cambios);
        return texto.Length <= 2000 ? texto : texto[..2000];
    }

    private class CambioPendiente
    {
        public EntityEntry Entry { get; set; } = null!;
        public string Accion { get; set; } = string.Empty;
        public string? EntidadId { get; set; }
        public string? Detalle { get; set; }
    }
}

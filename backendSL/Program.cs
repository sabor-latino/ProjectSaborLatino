using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

var builder = WebApplication.CreateBuilder(args);

// =============== Servicios ===============

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Errores en JSON estándar (ProblemDetails)
builder.Services.AddProblemDetails();

// Base de datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// La bitácora automática necesita saber qué usuario hace cada petición
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BitacoraInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
        .AddInterceptors(sp.GetRequiredService<BitacoraInterceptor>()));

// Identity: usuarios, roles y login con token
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization();

// Services del catálogo (Fase 1)
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IPaqueteService, PaqueteService>();
builder.Services.AddScoped<IEquipoService, EquipoService>();
builder.Services.AddScoped<IImagenGaleriaService, ImagenGaleriaService>();
builder.Services.AddScoped<IPreguntaFrecuenteService, PreguntaFrecuenteService>();

// Services de solicitudes y cotizaciones (Fase 2)
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<ICotizacionService, CotizacionService>();

// Services de eventos (Fase 3)
builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();
builder.Services.AddScoped<IEventoService, EventoService>();

// Services de usuarios y sistema (Fase 4)
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

// CORS para el frontend React
const string PoliticaFrontend = "Frontend";
var origenesPermitidos = builder.Configuration
    .GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(PoliticaFrontend, policy =>
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// =============== Datos iniciales ===============

await DbSeeder.SembrarAsync(app.Services);

// =============== Pipeline HTTP (el orden importa) ===============

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(PoliticaFrontend);
app.UseAuthentication();

// Valida en cada petición autenticada que el usuario siga activo en BD, que su SecurityStamp coincida y sincroniza sus roles actuales
var securityStampClaimType = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>()
    .Value.ClaimsIdentity.SecurityStampClaimType;

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tokenSecurityStamp = context.User.FindFirstValue(securityStampClaimType);
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenSecurityStamp))
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var estadoActual = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Activo,
                u.LockoutEnabled,
                u.LockoutEnd,
                u.SecurityStamp,
                Roles = (from ur in db.UserRoles
                         join r in db.Roles on ur.RoleId equals r.Id
                         where ur.UserId == u.Id && r.Name != null
                         select r.Name!).ToList()
            })
            .FirstOrDefaultAsync(context.RequestAborted);

        var bloqueado = estadoActual is not null &&
                        estadoActual.LockoutEnabled &&
                        estadoActual.LockoutEnd.HasValue &&
                        estadoActual.LockoutEnd.Value > DateTimeOffset.UtcNow;

        var selloRevocado = estadoActual is null ||
                            string.IsNullOrEmpty(estadoActual.SecurityStamp) ||
                            !string.Equals(tokenSecurityStamp, estadoActual.SecurityStamp, StringComparison.Ordinal);

        if (estadoActual is null || !estadoActual.Activo || bloqueado || selloRevocado)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (context.User.Identity is ClaimsIdentity identity)
        {
            foreach (var rolClaim in identity.FindAll(identity.RoleClaimType).ToList())
            {
                identity.RemoveClaim(rolClaim);
            }

            if (identity.RoleClaimType != ClaimTypes.Role)
            {
                foreach (var rolClaim in identity.FindAll(ClaimTypes.Role).ToList())
                {
                    identity.RemoveClaim(rolClaim);
                }
            }

            foreach (var rolActual in estadoActual.Roles)
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, rolActual));
            }
        }
    }

    await next(context);
});
app.UseAuthorization();

// Cierra /api/identity/register para que el registro público pase solo por POST /api/cuenta/registro (rol Cliente)
app.MapGroup("/api/identity")
    .MapIdentityApi<ApplicationUser>()
    .Add(endpointBuilder =>
    {
        if (endpointBuilder is RouteEndpointBuilder routeEndpoint &&
            routeEndpoint.RoutePattern.RawText?.TrimEnd('/').EndsWith("/register", StringComparison.OrdinalIgnoreCase) == true)
        {
            routeEndpoint.Metadata.Add(new ExcludeFromDescriptionAttribute());
            routeEndpoint.RequestDelegate = static context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            };
        }
    });
app.MapControllers();

app.Run();

public partial class Program { }

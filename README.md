# Sabor Latino

Repositorio único del proyecto, organizado en las siguientes carpetas:

| Carpeta            | Contenido                                                                 |
|--------------------|---------------------------------------------------------------------------|
| `backendSL/`       | API en ASP.NET Core (.NET 10) con EF Core                                 |
| `backendSL.Tests/` | Suite de pruebas de integración automatizadas en .NET 10 (xUnit + LocalDB aislado) |
| `frontendSL/`      | Interfaz web en React + Vite + TypeScript                                 |
| `dbSL/`            | Diagramas, datos de prueba y consultas SQL                                |
| `docs/`            | Documentación de integración (`docs/GUIA_INTEGRACION_FRONTEND.md`)        |

## Requisitos

- Visual Studio 2026 con la carga "Desarrollo de ASP.NET y web" (.NET 10 y SQL Server LocalDB)
- Herramienta de migraciones: `dotnet tool install --global dotnet-ef`
- Node.js LTS (para el frontend)

## Primera vez

```
git clone https://github.com/sabor-latino/ProjectSaborLatino.git
cd ProjectSaborLatino/backendSL
dotnet restore
dotnet user-secrets set "SuperAdmin:Email" "superadmin@saborlatino.com"
dotnet user-secrets set "SuperAdmin:Password" "<contraseña-local-segura>"
dotnet ef database update
```

## Backend

Abrir `ProjectSaborLatino.slnx` en Visual Studio y presionar F5, o desde terminal:

```
cd backendSL
dotnet run
```

La API queda en `https://localhost:7154` y `http://localhost:5238`.

## Pruebas automatizadas del Backend

La suite de integración crea y elimina automáticamente bases temporales aisladas `SaborLatinoDb_Test_<GuidN>` en SQL Server LocalDB sin tocar `SaborLatinoDb`:

```
dotnet test backendSL.Tests/ProjectSaborLatino.Tests.csproj
```

## Base de datos

Motor: **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`, base `SaborLatinoDb`).
Las tablas se crean y actualizan con las **migraciones de Entity Framework** en `backendSL/Migrations/`.
Después de cada `git pull`, si hay migraciones nuevas:

```
cd backendSL
dotnet ef database update
```

## Frontend

Ver `frontendSL/README.md` y la guía de contratos e integración con el backend en [docs/GUIA_INTEGRACION_FRONTEND.md](docs/GUIA_INTEGRACION_FRONTEND.md).

## Forma de trabajo en grupo

Antes de cambiar código, toda persona debe leer [CONTRIBUTING.md](CONTRIBUTING.md); los agentes deben leer además [AGENTS.md](AGENTS.md) y el registro de coordinación indicado allí. Las reglas cubren alcance, propiedad del frontend, protección de datos, pruebas, CI y revisión de Pull Requests.

Trabajar en una rama por tarea, verificar los cambios según la guía y abrir un Pull Request hacia `main`. La fusión requiere revisión y las comprobaciones de CI aprobadas una vez configuradas como obligatorias en GitHub.

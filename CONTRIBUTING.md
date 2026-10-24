# Guía de contribución — ProjectSaborLatino

Esta guía aplica tanto a las personas integrantes del equipo como a cualquier agente automatizado que colabore en el repositorio.

- **Para agentes IA (`Codex`, `Gemini` en Antigravity CLI `agy`, u otros):** es obligatorio leer primero `AGENTS.md` y, si existe en el entorno local, `COMUNICACION_CODEX_GEMINI.md` (bitácora operativa local no versionada en Git) antes de ejecutar cualquier acción. Para la integración entre frontend y backend, consultar `docs/GUIA_INTEGRACION_FRONTEND.md`.

---

## 1. Estructura y propiedad de áreas del repositorio

| Ruta | Descripción | Reglas de propiedad y alcance |
|---|---|---|
| `backendSL/` | API ASP.NET Core (.NET 10), EF Core e Identity | Todo cambio de comportamiento exige prueba automatizada de regresión en `backendSL.Tests/`. |
| `backendSL.Tests/` | Suite de pruebas de integración xUnit + `WebApplicationFactory` | Usa exclusivamente bases temporales aisladas `SaborLatinoDb_Test_<GuidN>`. |
| `frontendSL/` | Aplicación React 19 + Vite 8 + TypeScript | **Propiedad de la persona responsable del frontend.** Los agentes de backend no deben editar `frontendSL/` ni fusionar sus PRs. |
| `dbSL/` | Diagramas y artefactos de base de datos | No sobrescribir ni borrar diagramas o scripts preexistentes sin acuerdo explícito. |
| `docs/` | Documentación técnica e integración (`docs/GUIA_INTEGRACION_FRONTEND.md`) | Mantener sincronizada con los contratos reales del backend y las reglas de integración con `frontendSL/`. |
| `.github/` | Workflows de CI (`.github/workflows/ci.yml`), scripts y plantillas | Ejecuta puertas obligatorias de calidad para backend y frontend en cada `push` y `pull_request`. |

---

## 2. Reglas fundamentales de trabajo

1. **Respeto del alcance y de cambios ajenos:**
   - Trabajar en una rama dedicada por tarea (`feature/nombre-descriptivo` o `fix/nombre-descriptivo`).
   - Revisar `git status --short` antes y después de trabajar. Nunca revertir, limpiar (`git clean` / `git reset --hard`) ni mezclar cambios no relacionados.
2. **Prohibición de secretos:**
   - Nunca subir contraseñas, tokens, claves API ni credenciales en código, `appsettings*.json`, pruebas, logs ni descripciones de PR.
   - Para el desarrollo local del backend, configurar las credenciales iniciales del `SuperAdmin` mediante `dotnet user-secrets` en `backendSL/`:
     ```powershell
     cd backendSL
     dotnet user-secrets set "SuperAdmin:Email" "superadmin@saborlatino.com"
     dotnet user-secrets set "SuperAdmin:Password" "<contraseña-local-segura>"
     ```
3. **Aislamiento de la base de datos personal (`SaborLatinoDb`):**
   - Las pruebas automatizadas **nunca** deben conectarse ni modificar la base personal `SaborLatinoDb`.
   - `BackendTestFactory` crea por corrida una base efímera `SaborLatinoDb_Test_<GuidN>` en `(localdb)\MSSQLLocalDB`, aplica las migraciones reales de EF Core desde cero y la elimina en `DisposeAsync` (incluso si una prueba falla).

---

## 3. Verificaciones locales obligatorias antes de abrir un PR

### 3.1. Backend (`.NET 10` + `SQL Server LocalDB`)

Desde la raíz del repositorio en PowerShell:

```powershell
# 1. Restaurar paquetes
dotnet restore backendSL.Tests/ProjectSaborLatino.Tests.csproj

# 2. Verificar que no existan paquetes NuGet con vulnerabilidades conocidas
dotnet list backendSL/ProjectSaborLatino.csproj package --vulnerable --include-transitive
dotnet list backendSL.Tests/ProjectSaborLatino.Tests.csproj package --vulnerable --include-transitive

# 3. Compilar sin advertencias ni errores
dotnet build backendSL/ProjectSaborLatino.csproj --no-restore -warnaserror
dotnet build backendSL.Tests/ProjectSaborLatino.Tests.csproj --no-restore -warnaserror

# 4. Ejecutar toda la suite de integración sobre bases temporales aisladas
dotnet test backendSL.Tests/ProjectSaborLatino.Tests.csproj --no-build --no-restore

# 5. Si se modificaron entidades o configuración de EF Core, comprobar migraciones pendientes
dotnet ef migrations has-pending-model-changes --project backendSL/ProjectSaborLatino.csproj --no-build

# 6. Confirmar que no quedaron bases temporales SaborLatinoDb_Test_% en LocalDB
sqlcmd -S "(localdb)\MSSQLLocaldb" -d master -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name LIKE 'SaborLatinoDb%';"
```

### 3.2. Frontend (`frontendSL/`) y contrato `test:ci`

Desde la raíz del repositorio:

```powershell
# Ejecuta npm ci, npm run lint, npm run build y verifica el contrato test:ci
node .github/scripts/verify-frontend-ci.mjs
```

> Si ya tienes `frontendSL/node_modules` instalado localmente y deseas omitir `npm ci` durante una comprobación rápida sin tocar `node_modules`, puedes ejecutar:
> ```powershell
> node .github/scripts/verify-frontend-ci.mjs --skip-install
> ```

#### Alcance real de `lint` y `build` frente a pruebas futuras (`test:ci`)

- **Hoy:** `frontendSL/` cuenta con `npm run lint` (`eslint .`) y `npm run build` (`tsc -b && vite build`). Estas puertas verifican análisis estático, tipado TypeScript y empaquetado de producción con Vite, pero **no prueban interacciones de usuario en el navegador ni apariencia visual**, y actualmente `frontendSL/` aún no tiene una suite de pruebas automatizadas.
- **Contrato automático para suites de pruebas futuras (`test:ci`):**
  - El workflow `.github/workflows/ci.yml` delega en `.github/scripts/verify-frontend-ci.mjs` para no enumerar archivos, componentes, clases ni carpetas en el YAML.
  - Cuando se agregue cualquier herramienta de pruebas en el frontend (por ejemplo Vitest, Testing Library o Playwright), basta con definir en `frontendSL/package.json` el script no interactivo `"test:ci"` (por ejemplo `"test:ci": "vitest run"`). El verificador lo detectará y ejecutará automáticamente en cada `push` y `pull_request`, dejando que el propio runner descubra todas las pruebas.
  - Si alguien agrega archivos `*.test.*`, `*.spec.*` o dentro de `__tests__/` y olvida definir `"test:ci"` en `package.json`, el verificador **fallará en CI con código de salida `1`** para impedir que existan pruebas silenciosamente ignoradas.
  - **Paquetes anidados bajo `frontendSL/`:** si en el futuro se añaden subpaquetes con su propio `package.json` dentro de `frontendSL/**`, `verify-frontend-ci.mjs` los descubre recursivamente (excluyendo `node_modules`, `dist`, `build`). Cada paquete necesita un lockfile (`package-lock.json` o `npm-shrinkwrap.json`), `lint` y `build`. `test:ci` es obligatorio cuando el paquete contiene archivos de pruebas; si existe, se ejecuta siempre.

### 3.3. Formato y limpieza del diff

```powershell
git diff --check
```

---

## 4. Integración Continua (CI) y activación de bloqueo de merges en GitHub

El archivo `.github/workflows/ci.yml` se ejecuta en todos los eventos `push` y `pull_request` sin filtros de rutas y con permisos de solo lectura (`contents: read`), ejecutando dos jobs obligatorios:

1. **`Backend (.NET 10 + SQL Server LocalDB)`** en `windows-2022`: prepara `(localdb)\MSSQLLocalDB`, audita vulnerabilidades NuGet, compila con `-warnaserror`, ejecuta toda la suite `dotnet test` sobre bases efímeras aisladas y verifica que no queden bases `SaborLatinoDb*` residuales.
2. **`Frontend (Lint, TypeScript + Vite Build & Test Contract)`** en `ubuntu-latest`: instala Node 22, ejecuta `node .github/scripts/verify-frontend-ci.mjs` (`npm ci`, `npm run lint`, `npm run build` y contrato `test:ci`).

### Cómo hacer que GitHub bloquee técnicamente un merge si falla CI

Un archivo de workflow en `.github/workflows/ci.yml` ejecuta las comprobaciones, pero para que GitHub **impida fusionar un PR** cuando un job falle, un administrador del repositorio en GitHub debe activar la regla de protección de rama una vez que `.github/workflows/ci.yml` esté en `main` y haya corrido al menos una vez:

1. En GitHub (`sabor-latino/ProjectSaborLatino`), abrir **Settings → Rules → Rulesets** (o **Settings → Branches → Branch protection rules**).
2. Crear o editar la regla para la rama destino `main`.
3. Activar **Require a pull request before merging**.
4. Activar **Require status checks to pass** (y **Require branches to be up to date before merging**) y agregar como comprobaciones obligatorias (*Required checks*):
   - `Backend (.NET 10 + SQL Server LocalDB)`
   - `Frontend (Lint, TypeScript + Vite Build & Test Contract)`

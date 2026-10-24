## Resumen del cambio

- **Objetivo / Orden asociada:**
- **Área afectada:** [ ] `backendSL/`  [ ] `backendSL.Tests/`  [ ] `frontendSL/`  [ ] `dbSL/`  [ ] CI / Documentación

## Alcance y preservación de trabajo ajeno

- [ ] Este PR modifica **únicamente** los archivos dentro del alcance de la tarea.
- [ ] No revierte, sobrescribe ni mezcla cambios locales o ramas de otros integrantes.
- [ ] Si es un PR de backend/CI, el diff sobre `frontendSL/` está completamente vacío (`git diff --name-only origin/main...HEAD -- frontendSL`).
- [ ] No se incluyen secretos, contraseñas, tokens, cadenas de conexión privadas ni volcados de datos.

## Verificaciones ejecutadas antes de abrir el PR

> Adjuntar comando exacto y código de salida (`0` = éxito). No marcar como verificado un cambio que solo fue leído o que únicamente compiló.

### Backend (`.NET 10` + `SQL Server LocalDB`)

- [ ] `dotnet list backendSL/ProjectSaborLatino.csproj package --vulnerable --include-transitive` (sin vulnerabilidades)
- [ ] `dotnet build backendSL/ProjectSaborLatino.csproj --no-restore -warnaserror` (`0` advertencias, `0` errores)
- [ ] `dotnet build backendSL.Tests/ProjectSaborLatino.Tests.csproj --no-restore -warnaserror` (`0` advertencias, `0` errores)
- [ ] `dotnet test backendSL.Tests/ProjectSaborLatino.Tests.csproj --no-build --no-restore` (toda la suite pasa sobre bases temporales aisladas `SaborLatinoDb_Test_<GuidN>`, sin tocar `SaborLatinoDb`)
- [ ] Si hubo cambios en modelos de EF Core: migración generada, revisada y probada desde cero en base temporal aislada (`dotnet ef migrations has-pending-model-changes --project backendSL/ProjectSaborLatino.csproj`)

### Frontend (`frontendSL/`)

- [ ] `node .github/scripts/verify-frontend-ci.mjs` (`npm ci`, `npm run lint`, `npm run build` y contrato `test:ci`)
- [ ] Si se agregaron archivos `*.test.*`, `*.spec.*` o `__tests__/*`: el script `"test:ci"` está definido en `package.json` y ejecuta el runner de forma no interactiva.
- [ ] Si hubo cambios de interfaz/interacción: se comprobó manualmente el flujo en navegador (teniendo presente que `lint` y `build` validan tipos y empaquetado, no apariencia ni interacción).

### Higiene de Git

- [ ] `git diff --check` sin errores de formato ni espacios residuales.

## Riesgos y límites de la verificación

- Describe cualquier límite conocido, supuesto de negocio o comprobación que requiera validación adicional:

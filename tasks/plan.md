# Plan Técnico en Vivo — Fase 5: Login Privado en Dirección Independiente + Continuidad Multi-IA

## Estado de Ejecución de la Fase 5 (Checklist Granular)

- [x] **Paso 5.0:** Grabar ADN de personalidad, reglas estrictas y protocolo de relevo multi-IA en `AGENTS.md`, `CLAUDE.md`, `.cursorrules`, `.github/copilot-instructions.md`, `tasks/plan.md` y `donde lo dejamos.md`.
- [x] **Paso 5.1:** Crear `frontendSL/src/utilidades/portal.ts` y actualizar `frontendSL/package.json` y `frontendSL/vite.config.ts` con scripts para Dirección Pública (`dev` en `54526`, `build:public` -> `dist-public`) y Dirección Privada Administrativa (`dev:admin` en `54527`, `build:admin` -> `dist-admin`).
- [x] **Paso 5.2:** Actualizar `frontendSL/src/App.tsx`, `Layout.tsx`, `Login.tsx`, `Inicio.tsx` y `SolicitarCotizacion.tsx` para ocultar 100 % el login/registro del sitio público (`54526` devuelve `404` en `/login` y `/panel`) y aislar el Login Privado Administrativo en su dirección independiente (`54527` / `admin.*`).
- [x] **Paso 5.3:** Actualizar `backendSL/appsettings.Development.json` para permitir CORS desde `http://localhost:54527` y `https://localhost:54527`, y reiniciar el servidor Backend en vivo (`http://localhost:5238`).
- [x] **Paso 5.4:** Ampliar pruebas automatizadas en `frontendSL/src/__tests__/app-smoke.test.tsx` (verificando Modo Público sin login y Modo Admin con login privado) y aprobar `npm run lint`, `npm run test:ci`, `npm run build:public`, `npm run build:admin`, `.githooks/pre-commit.ps1` y `.githooks/pre-push.ps1`.
- [x] **Paso 5.5:** Levantar simultáneamente Sitio Público (`http://localhost:54526`) y Portal Administrativo Privado (`http://localhost:54527`), auditar ambos en vivo con Chrome DevTools MCP (`0` errores de consola y red), capturar pantallas, abrir ambas direcciones en el navegador real del usuario y marcar todo `[x]` en `donde lo dejamos.md`.



## Fase 6 — Correcciones post-auditoría (estado en vivo)

- [x] **6.1** Separar portales en fragmentos (`AppPublico.tsx` / `AppAdmin.tsx`) con `React.lazy` y constantes `SOLO_PUBLICO` / `SOLO_ADMIN` en `portal.ts`.
- [x] **6.2** Pruebas Vitest adaptadas (4/4) y `npm run lint` limpio.
- [x] **6.3** Confirmar con grep que `dist-public` no contiene código ni textos del admin y `dist-admin` no contiene la vitrina.
- [x] **6.4** Backend galería: `TituloEn`, validación de URL http/https, migración `GaleriaTituloEn`, `GaleriaAdminTests.cs` (subagente `f9d31e94-673f-42ea-be57-3814aa3bb990`).
- [x] **6.5** Frontend galería: `/panel/galeria` en el menú, campo `Título (Inglés)`, galería pública bilingüe, `tipos.ts`.
- [x] **6.6** Auditoría independiente de sabor_qa completada sin hallazgos Required; revisión externa adicional registrada aparte.
- [x] **6.7** Hooks `pre-commit.ps1` y `pre-push.ps1` en `EXIT 0`; 0 bases `SaborLatinoDb_Test_%`.
- [x] **6.8** Migrar `SaborLatinoLiveDemoDb` (jamás `SaborLatinoDb`), reiniciar servidores, verificar `54526` y `54527` con DevTools (0 errores) y abrir ambas en el navegador.

### Cierre de Fase 6 en Vivo (2026-11-01 17:45)
- LocalDB reparado y en ejecución (`MSSQLLocalDB`). 0 bases temporales residuales.
- Backend: 69/69 pruebas xUnit aprobadas.
- Frontend: 7/7 pruebas Vitest aprobadas, lint limpio, builds separados (`dist-public` y `dist-admin`).
- Hooks de Git: `pre-commit.ps1` y `pre-push.ps1` en `EXIT 0`.
- Chrome DevTools MCP: Verificado en vivo en `54526` (público sin login, 404 estricto) y `54527` (panel admin autenticado, `/panel/galeria` bilingüe probado) con 0 errores de consola.
- Navegador real: Ambas direcciones abiertas para el usuario.

## Sesión de cierre de Fase 6 — 2026-11-01 (en ejecución)

El usuario autorizó completar los pasos pendientes con subagentes. Backend revisa pruebas y migraciones de galería y actualizará exclusivamente SaborLatinoLiveDemoDb. Frontend separa los módulos de los portales y completa la galería ES/EN. QA independiente refuerza CI y audita artefactos, hooks y navegador. Pruebas y cierre pendientes; no se marcan casillas nuevas todavía.

Chrome DevTools MCP no está expuesto y CUA informa que el uso del navegador está deshabilitado. Se prepara verificación funcional con Playwright y Chromium local. Sin commit, push, merge ni despliegue de producción.

## Resultado técnico de Fase 6 — 2026-11-01

Pasos 6.3 a 6.7 completados con subagentes sabor_backend, sabor_frontend y sabor_qa. El principal ejecutó los parches y comandos preparados por ellos porque las solicitudes de escalación de los subagentes quedaron pendientes; se conservaron los cambios preexistentes.

- Backend: dotnet test, 69/69 correctas; modelo EF sin cambios pendientes. GaleriaTituloEn aplicada exclusivamente a SaborLatinoLiveDemoDb; historial con ocho migraciones y columna TituloEn nvarchar(150). No se accedió a la base personal.
- Frontend: 7/7 pruebas. Regresión roja demostrada antes de añadir ruta/menu, edición del título inglés y traducción de fotos de referencia; después, guardado observable y cierre de diálogo, títulos/alt ES/EN y fallback español correctos.
- Lint y compilaciones genérica, pública y administrativa correctas. Los modos fijos se resuelven en compilación; el plugin informa módulos de TODOS los chunks emitidos.
- CI comprueba 24 módulos fuente públicos y 33 administrativos, sin páginas del portal contrario; el paquete público no contiene Portal Administrativo. Las utilidades de autenticación y preferencias compartidas no se consideran páginas del panel.
- QA independiente sin hallazgos Required pendientes en código y artefactos. El auditor rechazó seis casos negativos y aceptó el caso válido.
- Hooks pre-commit y pre-push: EXIT 0. Backend compilado con cero advertencias y errores; después de la suite del hook, consulta independiente SQLCMD EXIT 0: cero bases SaborLatinoDb_Test_% residuales. El hook ahora falla también cuando no puede consultar SQLCMD.
- API de demostración reiniciada: PID 19876, http://localhost:5238 y https://localhost:7154, conexión explícita a SaborLatinoLiveDemoDb. Registros temporales: sabor-demo-878fb6991fe14332a62b060de7e5afb8.out.log y .err.log.
- Comprobación HTTP final independiente: sitio público http://localhost:54526/ 200; portal admin http://localhost:54527/ 200; /api/galeria 200 (vacía); /api/servicios 200. CORS administrativo se comprobó correcto.

Paso 6.8 PARCIAL: migración y reinicio completados; auditoría visual, consola, red y apertura visible pendientes. Chrome DevTools MCP no está expuesto. CUA sigue mostrando Local browser use is disabled in Work settings y pipe nativo no disponible. El usuario eligió habilitar el navegador de Work; esa selección no cambió aún el ajuste. No se ejecutó Playwright porque el usuario eligió Work. No se afirma cero errores del navegador ni fase 6 completamente terminada.

Revisión adicional solicitada: Gemini CLI oficial 0.66.0 se ejecutó con paquete de código sin credenciales, modo plan y política deny de herramientas. El cliente fue rechazado durante autenticación con IneligibleTierError / UNSUPPORTED_CLIENT (servicio solicita migrar a Antigravity); no produjo revisión. La configuración aislada del proceso también fue omitida por el CLI al fallar su comprobación Get-Acl; no se alteraron ajustes globales y no se ejecutaron herramientas del revisor. Paquete reproducible: C:/Users/ownne/AppData/Local/Temp/sabor-gemini-review-42de870212f6440aa4b8b78f9970ddc8/review.txt. El usuario eligió investigar Antigravity; resultado documentado al final.

Aviso no bloqueante: herramientas EF 10.0.11 frente a runtime 10.0.12; comprobaciones y migración finalizaron correctamente. Sin commit, push, merge ni despliegue de producción.

### Investigación adicional de Antigravity — 2026-11-01

El usuario eligió investigar la revisión con Antigravity. Se localizó el CLI independiente C:/Users/ownne/AppData/Local/agy/bin/agy.exe, versión 1.4.7; su ayuda y la consulta de modelos finalizaron correctamente y mostraron Gemini 4 Argon (High). El comando antigravity.cmd del PATH es un lanzador del IDE, distinto de este CLI.

Se preparó un intento con el paquete de código aislado. El CLI advirtió que combinar --mode plan con --disable-slash-commands anula el modo plan, y el evento inicial mostró permission_mode always-proceed. El principal canceló el intento inmediatamente; el registro solo contiene init y user_input, sin llamadas de herramientas ni respuesta de revisión. No se atribuye dictamen a ese intento.

La documentación de modos confirma que plan antepone /plan y que las reglas explícitas y permisos globales conservan precedencia. La configuración existente tiene permisos amplios y hay dos sesiones agy del usuario activas. La investigación independiente de sabor_backend no encontró un mecanismo temporal documentado que garantice lectura exclusiva sin alterar ese perfil. No se modificaron permisos globales ni sesiones ajenas.

Resultado: investigación completada, cliente y modelos disponibles, paquete reproducible preparado; revisión externa adicional NO realizada. Continúa vigente la auditoría de sabor_qa y las verificaciones 69/69 backend, 7/7 frontend y hooks EXIT 0. El paso 6.8 permanece pendiente del navegador Work.

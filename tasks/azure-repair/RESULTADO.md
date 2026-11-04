# Reparación Azure completada — 2026-11-03

La API y el despliegue automático están publicados y verificados. [PR20 fusionado con autorización expresa](https://github.com/sabor-latino/ProjectSaborLatino/pull/20), main `62e0d6a1c3eb978cffe447c39fa7480db17275df`.

## Fallos corregidos

1. Huella SQL dependiente del orden de Path en Windows/Linux: script canónico, selección recursiva, extensión C# normalizada, UTF8/BOM y saltos uniformes. Doce regresiones ejecutadas en ambas plataformas.
2. Baseline de ocho migraciones ante nueve fuentes: se verificó primero Azure SQL, se aplicó solamente la migración de WhatsApp y se comprobó después el esquema completo. Solo entonces se actualizó el baseline a `0b4da99686e70763b46f7daf9cccd8c1c5bc04c22011427c511f5b32532329f8`.
3. Confianza OIDC antigua: al superar el control SQL, el primer intento real llegó a Azurelogin y reveló AADSTS700213. GitHub emitía un subject con IDs inmutables y la FIC esperaba nombres sin IDs. Se actualizó únicamente el subject de `github-main` en la identidad existente, conservando issuer, audiencia, principal y único rol Website Contributor limitado a este App Service. Sin secretos nuevos ni permisos SQL para el runtime de despliegue.

Subject actual, limitado a este repositorio y main:

```text
repo:sabor-latino@343788676/ProjectSaborLatino@1389433606:ref:refs/heads/main
```

Referencias: [OIDC GitHub](https://docs.github.com/en/actions/reference/security/oidc), [formato con IDs inmutables](https://github.blog/changelog/2026-04-23-immutable-subject-claims-for-github-actions-oidc-tokens/).

Rectificación del informe inicial: los escapes duplicados eran representación JSON. El blob decodificado usa correctamente NUL; no se atribuye ese supuesto defecto a la solución.

## Producción SQL y recuperación

- Destino Azure validado por servidor, catálogo y EngineEdition5. La base personal LocalDB no participó.
- Antes: ocho IDs exactos, correo obligatorio, sin los cuatro campos/índice nuevos; una solicitud existente.
- Delta aplicado el2026-11-03 a13:36:53–54UTC. SQL revisado SHA256 `C181C2CAA8AC1CA96C060571350B958F278AFC761C2C3F70F5941112817FBF4F`, con transacción, XACT_ABORT, TRY/CATCH/rollback/THROW, esquema dbo y límite de bloqueo.
- Después: nueve IDs exactos, cinco columnas con tipos/longitudes/nulabilidad correctos, índice único filtrado activo sobre ClaveIdempotencia. Solicitudes1→1.
- Reglas temporales de IP individuales retiradas y ausencia comprobada al cierre. Se conservaron reglas previas y permisos existentes.
- Retención PITR7días acreditada; no se ensayó restauración. Se conservó un ZIP del paquete anterior, SHA256 `D67AD3822FBFA4AF240821AA85ECCB7D1606A750D135F9B9060C0414F1B3CA4A` en artefactos locales ignorados. No se ejecutó Down destructivo.

## Evidencia de publicación

| Etapa | Resultado |
|---|---|
| [CI rama](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41647261461) | success |
| [CI PR20](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41647268262) | success |
| [CI main62e0d6a](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41647996591) | success |
| [Deploy automático, intento2](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41648425222/attempts/2) | success |

En el intento automático final pasaron main exacto/CI, huella SQL, publish, Azurelogin OIDC, webapps-deploy y verificación HTTP/CORS. Job125962164961. Kudu final `83679e11c78e4730a3232c7fc73e0dfc`, status4, active/complete, OneDeploy. El intento1 queda conservado como evidencia del fallo OIDC descubierto y corregido.

Antes del automático se recuperó la compatibilidad de la API mediante publicación manual de la appmain161531e, Kudu286d55aa15af4e6ea311c1238bb426c1. Se comprobó coincidencia exacta del binario publicado con el paquete revisado. El cierre final corresponde al automático del main62e0d6a, no solo a aquella publicación manual.

## Pruebas y alcance

- Local:81 backend,24 frontend,12 regresiones Python, lint, TypeScript, builds público/admin y separación26/35módulos; ambos hooks exit0. Python y pruebas de aplicación también verdes en GitHub.
- Probes de API sin crear solicitudes: body{} responde400 con NombreContacto/TipoEvento/FechaEvento/Lugar requeridos y sin correo obligatorio; la marca WhatsApp sin JWT responde401. El adaptador decodificó correctamente UTF8 de ProblemDetails antes de comprobar esos campos.
- [QA tras automático](../qa-azure/RESULTADO.md): cinco rutas, ocho vistas1440/390 y seis capturas revisadas; consola sin errores/advertencias, red sin fallos/pendientes, sin overflow. Cinco GET200 con CORS exacto y OPTIONS204 de ambos portales.
- Sin login ni solicitudes válidas de producción creadas, sin navegar WhatsApp ni enviar mensajes. La creación/idempotencia/recepción completas están cubiertas por pruebas aisladas y el E2E demo previo; no se atribuyen al smoke de lectura en producción.
- Auditoría frontend de producción:0vulnerabilidades. Doce avisos moderados en tooling de desarrollo dependen de minimatch10; no hay parche10 compatible y forzar11 altera los rangos de ESLint. Quedan registrados como mantenimiento separado, no como fallos del despliegue. Continúan advertencias de deprecación de acciones Node20; los jobs terminaron correctamente usando el runtime admitido por GitHub.

## Archivos y relevo

Se publicaron únicamente cinco archivos.github y el runbook docs/AZURE_CICD_REPAIR.md. Se preservaron cambios y servidores anteriores. Los scripts operativos, metadatos SQL, backups y logs locales están en tasks/azure-repair; los artefactos quedan ignorados por Git. Los seis archivos fuente locales se compararon por hash de blob con origin/main y coincidieron.

Al cierre se conservaron los seis archivos propios ya publicados en un checkpoint Git y se seleccionó origin/main62e0d6a en la rama local fix/azure-repair-verified. El código versionado local coincide con la revisión publicada; solo quedan documentos de relevo y evidencias locales. El checkpoint es histórico y no debe reaplicarse sobre main. Ver .artifacts/local-checkpoint.json; no se descartaron documentos operativos ni cambios ajenos.

La apertura visible de Chrome por terminal fue rechazada por revisión automática («blocked by policy») y CUA informó «Local browser use is disabled in Work settings». No se alteraron esas preferencias ni se sorteó la restricción. Las pruebas Chrome DevTools aisladas autorizadas y sus capturas reales sí se completaron.

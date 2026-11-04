> RECTIFICACIÓN: la hipótesis de escapes duplicados era incorrecta; el blob decodificado usa NUL correctamente. Ver apartado de rectificación. El usuario ya autorizó aplicar la reparación; trabajo activo en C:/Users/ownne/source/repos/SaborLatino-azure-fix.

# Diagnóstico del despliegue automático Azure — 2026-11-03

Estado: análisis terminado; solución NO aplicada. Solicitud del usuario: explicar el fallo y entregar el reporte antes de corregir. No se modificaron workflows, código de aplicación, permisos, firewall, esquema ni datos; tampoco se relanzó ninguna ejecución.

## Conclusión

El job se detiene en `Require reviewed Azure SQL schema baseline` con exit1. La publicación .NET se completa y las etapas de inicio de sesión Azure, despliegue y verificación HTTP quedan omitidas. La validación de la huella depende del orden de rutas del sistema operativo. Además, la revisión actual incorpora una novena migración y el control aún declara una base de ocho.

La comprobación defectuosa ya bloqueó la primera ejecución con ocho migraciones, antes de añadir WhatsApp. Por tanto, atribuir todo el incidente únicamente a la nueva migración sería incompleto.

## Ejecuciones comprobadas

Horas UTC para evitar ambigüedad: Costa Rica=UTC−6.

| Ejecución | Revisión | Resultado |
|---|---|---|
| [CI34](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41585452380), 2026-11-03 05:42–05:44 | `161531e55281d7adef3cfe6725b55c10b783faaf`, mergePR19 | success |
| [Deploy Azure2](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41585695416), intento1, 05:44–05:45 | misma revisión | failure, paso6 |
| [Deploy Azure2, intento2](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41585695416/attempts/2), 05:48 | misma revisión | failure, paso6; job125751701311 |
| [Deploy Azure1](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41490845007), 2026-11-02 17:24–17:25 | `5428206e2008e2c02cd000deb4f3910bf361d860`, mergePR18 | failure, mismo paso6 con ocho migraciones |

Último job: checkout correcto, validación de SHA/CI correcta, setup-dotnet correcto, `dotnet publish ... -warnaserror` correcto, control de esquema fallido. `azure/login`, `azure/webapps-deploy` y pruebas CORS posteriores no se ejecutaron. Error relevante: «Azure SQL migration baseline changed».

El flujo automático quedó configurado pero estas ejecuciones no completaron una publicación automática. La validez efectiva de OIDC/permisos de publicación no queda acreditada por pasos omitidos.

## Causa1: orden distinto entre Windows y Linux

En el [workflow exacto](https://github.com/sabor-latino/ProjectSaborLatino/blob/161531e55281d7adef3cfe6725b55c10b783faaf/.github/workflows/deploy-azure.yml) se utiliza `sorted(Path(...).glob('*.cs'))`. Python compara rutas con la semántica de cada plataforma. Windows no distingue mayúsculas para esta comparación; POSIX sí. Por ejemplo, `InitialCreate.cs` y `InitialCreate.Designer.cs` cambian de orden entre ambas plataformas.

La huella esperada se reprodujo EXACTAMENTE usando orden Windows y separador NUL. Ese mismo conjunto de ocho migraciones produce otra huella con orden POSIX. Referencia oficial: [pathlib y propiedades generales](https://docs.python.org/3/library/pathlib.html#general-properties).

## Rectificación de la hipótesis sobre escapes

La revisión independiente posterior decodificó el blob remoto exacto634b252e72eea5dc30f1da829bc9c2f39c0e4519 y analizó el literal con AST: el separador real es un solo byte NUL (hex00). Los escapes duplicados vistos en la salida anterior eran la representación JSON. NO existe ese segundo defecto. La causa confirmada es ordenación Windows/POSIX, junto con baseline antiguo frente a migración nueva. Las variantes con barra+cero del archivo de reproducción son contrafactuales y no describen lo que ejecuta el workflow.

## Reproducción simbólica independiente de archivos de trabajo

Se leyeron blobs Git, sin checkout ni mutaciones. `python tasks/diagnostico-azure/repro-huella.py` reproduce combinaciones de orden y separador con `PureWindowsPath`/`PurePosixPath`.

| Fuente | Orden / separador | SHA256 |
|---|---|---|
| Ocho migraciones, referencia esperada | Windows / NUL | `3cc2085bc01e225b3f8659d58c4482c34657aa1848626e1f46cdbd58ba37fe8a` |
| Ocho migraciones, equivalente al runner actual | POSIX / NUL | `d64812a4b49799b866809a69e2ddb4a292d33c29d057cc7626682d88fb5a0f4d` |
| Ocho migraciones, propuesta canónica aún SIN aplicar | POSIX / NUL | `d64812a4b49799b866809a69e2ddb4a292d33c29d057cc7626682d88fb5a0f4d` |
| Nueve migraciones, equivalente al runner actual | POSIX / NUL | `0b4da99686e70763b46f7daf9cccd8c1c5bc04c22011427c511f5b32532329f8` |
| Nueve migraciones, propuesta canónica aún SIN aplicar | POSIX / NUL | `0b4da99686e70763b46f7daf9cccd8c1c5bc04c22011427c511f5b32532329f8` |

El árbol Git de migraciones de main remoto161531e y del commit local14a5042 es idéntico: `f384c66ecef67e5981a445b4ac4ac6386b82d8a5`. Esto permite reproducir los hashes de main con los blobs locales exactos, sin confiar en archivos modificados. El baseline anterior2d8dab3 y main5428206 tienen las mismas ocho migraciones y las mismas huellas en la reproducción.

Los hashes propuestos son evidencia de diagnóstico, NO autorización para actualizar la huella de producción sin verificar SQL.

## Causa2: evolución real del esquema

PR19 añadió `20261102190453_SolicitudWhatsAppManual` y su Designer, y modificó el snapshot. El workflow conserva la huella y el comentario de ocho migraciones. La novena migración:

- Permite null en `Solicitudes.CorreoContacto`.
- Añade `ClaveIdempotencia`, `HuellaIdempotencia`, `RecibidoWhatsAppEn` y `RecibidoWhatsAppPorUsuarioId` como columnas opcionales.
- Crea el índice único filtrado `IX_Solicitudes_ClaveIdempotencia`.

El Up no borra solicitudes; aun así, `ALTER COLUMN` e índice pueden tomar bloqueos y requieren revisión y una ventana apropiada según tamaño/carga. El Down elimina esos campos y puede perder evidencia de recepción/idempotencia; volver a correo obligatorio puede sustituir valores null por vacío según el SQL generado y exige revisar esa pérdida semántica. No es una estrategia inocua de rollback de datos.

La huella del workflow solo inspecciona archivos de código; no consulta `__EFMigrationsHistory` ni certifica el esquema real de Azure SQL.

## Estado de servicios y límites

- Azure App Service: Running, habilitado, runtime .NET10; última modificación indicada por ARM `2026-11-02T17:00:32.973333`.
- SQL pasó de Resuming a Online durante el diagnóstico; AutoPause y AlwaysOn=false se conservaron. Primer GET servicios agotó50s durante el arranque; la repetición respondió200. Paquetes/galería/promociones respondieron200.
- GET servicios con Origin público y administrativo respondió200 y devolvió CORS exacto para cada dominio. Esto prueba disponibilidad de esos catálogos, no la función nueva de solicitudes.
- GitHub informa success para ambos despliegues Vercel del commit161531e. Hay riesgo de frontend nuevo con API sin esa publicación: formularios con correo opcional/idempotencia pueden no corresponder al backend disponible. No se crearon solicitudes de producción para probarlo.
- Intento de consulta de SOLO metadatos de Azure SQL con la sesión Entra existente: bloqueado por firewall de IP de este equipo. No se cambió firewall ni permisos. Por ello NO se afirma cuántas migraciones hay aplicadas actualmente en producción. La base personal LocalDB nunca se utilizó.
- Advertencias `punycode`/acciones Node20 deprecadas aparecen en pasos exitosos. No son el error que detiene este job; su mantenimiento se puede tratar después.
- No se validó una autenticación/publicación OIDC real porque los jobs fallan antes. Tras arreglar el control podrían aparecer errores posteriores todavía no ejercitados.

## Solución propuesta, pendiente de aplicar tras este reporte

1. Verificar en Azure SQL, desde un acceso ya permitido, la historia de migraciones, las cinco columnas relevantes y el índice; confirmar el esquema real antes de cambiar el baseline. Si hace falta abrir acceso temporal, hacerlo acotado y retirar luego la regla; esto no se hizo en el diagnóstico.
2. Generar/revisar el SQL de la migración pendiente para la revisión exacta; comprobar copia/restauración y probarlo en un entorno aislado. Aplicar solo las migraciones faltantes en Azure. [Guía oficial EF para aplicar migraciones](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).
3. Extraer la huella a un script único y determinista: ordenar por nombre POSIX explícito (strings), normalizar BOM/CRLF/LF y usar `bytes([0])` para evitar escapes ambiguos. Añadir regresiones de orden Windows/POSIX, saltos/BOM y detección de una migración nueva.
4. Solo después de verificar el esquema revisado, actualizar el baseline correspondiente y mantener la protección. No borrar el control ni sustituirlo por un valor calculado automáticamente que siempre se acepte.
5. Ejecutar CI de la revisión final; después desplegar exclusivamente esa revisión de main. Comprobar OIDC, publicación, salud/CORS y el flujo WhatsApp/panel con datos de prueba acordados. Reintentar ahora sin cambios reproduce el fallo.

Este plan conserva la separación entre permisos de publicar App Service y permisos de cambiar SQL. Automatizar migraciones futuras requiere una etapa/identidad de migración revisada; no se resuelve otorgando privilegios SQL amplios a la identidad de runtime.

## Evidencias y método

Conector GitHub: lista de ejecuciones, jobs de ambos intentos actuales, logs del último y del primero histórico, workflow de SHA exacto, árbol Git y estados Vercel. Azure CLI: solo consultas ARM y solicitud de token transitorio para intentar los SELECT de metadatos. HTTP: GET de catálogos, sin POST ni mensajes reales. Script [repro-huella.py](repro-huella.py), salida [huellas-reproducidas.json](huellas-reproducidas.json), consulta reproducible [consultar-esquema.ps1](consultar-esquema.ps1) (bloqueada por firewall).

Se intentó reactivar agente backend y crear un sustituto; el servicio respondió `agent thread limit reached`. Diagnóstico realizado directamente y contrastado con reproducción simbólica y documentación oficial. Ninguna revisión delegada nueva se da por realizada.

Contexto Git observado al iniciar: worktree limpio, rama `feature/solicitudes-whatsapp-manual`, HEAD14a5042 ya confirmado por trabajo externo y PR19 ya fusionado en main161531e. No se hizo pull/reset/clean/commit/push/merge durante este diagnóstico. Cambios locales de esta tarea: solo informe, scripts/resultados de diagnóstico y relevo.

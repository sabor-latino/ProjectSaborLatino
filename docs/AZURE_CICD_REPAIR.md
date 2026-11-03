# Reparación del control de esquema y despliegue Azure

El despliegue de main compilaba correctamente pero se detenía antes de Azurelogin porque la huella se había calculado en Windows y el runner la recalculaba en Linux. Ordenar objetos Path usa semántica de plataforma: cambia el orden entre una migración.cs y su Designer.cs. La revisión161531e también añadió una novena migración mientras el baseline conservaba ocho.

La primera hipótesis del diagnóstico sobre escapes duplicados se descartó: el blob remoto decodificado634b252e72eea5dc30f1da829bc9c2f39c0e4519 usa un byte NUL correctamente. Esa hipótesis procedía de interpretar la representación JSON del contenido.

## Control corregido

- `.github/scripts/azure_schema_baseline.py` ordena strings de rutas relativas POSIX de forma explícita.
- Incluye archivos C# en subcarpetas y extensiones `.cs`/`.CS` con la misma selección en ambas plataformas, además de Designer y snapshot.
- Normaliza UTF8-BOM, CRLF, CR y LF; usa un byte NUL como separador.
- Rechaza ausencia de fuentes, baseline inválido o cualquier diferencia. No consulta ni modifica bases de datos.
- `.github/azure-sql-baseline.sha256` contiene una huella revisada, nunca se actualiza automáticamente para aceptar el estado corriente.
- El workflow valida la huella antes de publicar. Conserva guardas de main exacto, CI exitoso y permisos OIDC acotados.
- CI ejecuta doce regresiones del algoritmo en Windows y Linux. El stream esperado de la prueba de orden está construido independientemente del algoritmo.

Verificación local:

```text
python -B -m unittest discover -s .github/scripts/tests -p test_azure_schema_baseline.py -v
python -B .github/scripts/azure_schema_baseline.py
```

## Baseline revisado el2026-11-03

Se acreditó en Azure SQL la historia de ocho migraciones, correo obligatorio y ausencia de los cuatro campos/índice nuevos. Se aplicó únicamente el delta20261101205742→20261102190453 con transacción, XACT_ABORT, TRY/CATCH, rollback y límite de espera de bloqueo.

Después se verificaron los nueve IDs de migración, correo nullable nvarchar256, clave uniqueidentifier nullable, huella nvarchar64 nullable, recepción datetime2(7) nullable, autor nvarchar450 nullable e índice único filtrado activo sobre ClaveIdempotencia. La solicitud existente se conservó (recuento1→1). Las reglas de firewall temporales individuales fueron retiradas; no se alteraron reglas anteriores ni permisos de runtime/OIDC.

La fuente revisada termina en `20261102190453_SolicitudWhatsAppManual`. Huella canónica:

```text
0b4da99686e70763b46f7daf9cccd8c1c5bc04c22011427c511f5b32532329f8
```

Solo se usó el servidor Azure explícito del proyecto. La base personal LocalDB no participó. La retención PITR consultada era7días; esto no acredita un ensayo de restauración. Se conservó por separado un ZIP del paquete de aplicación previo.

## Siguientes cambios de esquema

1. Revisar SQL generado y recuperación antes de aplicarlo. Confirmar servidor Azure, catálogo, EngineEdition e IDs exactos del historial.
2. Aplicar únicamente migraciones faltantes mediante una identidad con permisos de esquema adecuados, manteniendo el runtime separado.
3. Verificar columnas, nulabilidad, índices e historial real. Solo entonces actualizar el baseline revisado.
4. Ejecutar CI y publicar la revisión exacta de main. Comprobar autenticación Azure, salud/CORS y comportamiento afectado.

El control de huella acredita fuentes revisadas, no inspecciona el esquema real. No desactivarlo para solucionar diferencias. No ejecutar Down como rollback automático: elimina evidencia de recepción/idempotencia y puede sustituir correos null. Preferir una recuperación de aplicación compatible con el esquema ampliado.

## Validación y límites

Antes de publicar se aprobaron81 pruebas backend,24 frontend, lint, tipos, builds público/admin, separación de módulos y hooks precommit/prepush; doce regresiones Python y guard real correctos. El baseline de navegador de solo lectura observó cinco rutas y ocho vistas sin errores finales de consola/red/overflow; el arranque frío inicial de SQL se registró aparte.

La auditoría de dependencias de producción frontend no detectó vulnerabilidades. Hay doce avisos moderados de tooling derivados de minimatch10; la corrección11 rompe los rangos actuales de ESLint. No se introdujo un override mayor ni `npm audit fix --force` durante esta reparación. No se atribuyen pruebas de creación o recepción de mensajes reales en producción a un smoke de lectura.

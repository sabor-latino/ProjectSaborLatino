# Resultado QA — WhatsApp manual — 2026-11-02

Chrome DevTools MCP oficial 1.12.0 por stdio, Chrome 156 headless con perfil temporal
aislado. El principal abrió además Chrome visible. No se usó CUA, no se tocaron
sesiones personales y no se envió ningún mensaje real. Servidores del worktree:
público 54528, admin 54529, API 5239; principal confirmó exclusivamente
SaborLatinoLiveDemoDb. No se ejecutaron builds/suites de aplicación desde QA.

## Flujo real aprobado

- [E2E integrado](.artifacts/1793653992461-integration.json), EXIT 0: solicitud demo
  #3 creada por UI con teléfono y correo vacío, POST 201; referencia y resumen
  codificados con acentos, saltos, # y &; salida wa.me bloqueada deliberadamente.
- Nueva navegación y recarga recuperaron el mismo registro sin otro POST. ES/EN
  conservó referencia. Login demo mediante UI y confirmación manual en panel:
  muestra primera fecha/autor y estado comercial Pendiente. Sin cotización enviada.
- [Fallo de transporte](.artifacts/1793653936511-integration.json), EXIT 0: Offline
  deliberado conserva formulario y muestra error; no presenta éxito ni abre chat.
  Una solicitud API fallida y un error de consola son esperados en ese escenario.
- Primeros fallos del adaptador se resolvieron sin modificar la aplicación:
  UID de label estático, datetime-local sin rellenar, incompatibilidad MCP de Offline
  con blocklist, y retorno antes de completar el POST. Una prueba incompleta dejó
  la solicitud artificial #2; #3 es la ejecución integral aprobada. No se borraron.
- Auditoría independiente detectó replay que revelaba datos administrativos y
  pérdida de recuperación al salir: ambos corregidos por los agentes propietarios,
  con pruebas de regresión. Sin Required pendientes en la revisión final.

## Consola/red limpia, medida por separado

Tras esperar fuentes e imágenes, procesos Chrome nuevos sin fallos inducidos:

| Portal | Solicitudes registradas | Errores/warnings consola | Fallos red | Pending |
|---|---:|---:|---:|---:|
| [Público /solicitar](.artifacts/smoke-54528-1793654238471.json) | 48 | 0/0 | 0 | 0 |
| [Admin /login](.artifacts/smoke-54529-1793654238773.json) | 53 | 0/0 | 0 | 0 |

El E2E conserva aparte el bloqueo deliberado de wa.me (`ERR_INTERNET_DISCONNECTED`).
Una carga de imagen abortada por cambio de emulación aparece en el registro visual;
el smoke independiente cargó ese recurso correctamente. No se mezclan con API rota
ni se afirma cero errores absolutos en sesiones donde se indujeron fallos.

## Capturas finales observadas

[Reporte visual](.artifacts/1793654221037-visual.json), EXIT 0. Para repetir sin crear
solicitudes se rehidrató solo el almacenamiento del registro artificial #3 del E2E;
no es otra prueba de persistencia. Capturas reales con contenido visible y scrollY=0:

- [Formulario 1440×900](.artifacts/1793654221037-formulario-1440.png).
- [Éxito escritorio ES](.artifacts/1793654221037-exito-1440.png) y
  [EN](.artifacts/1793654221037-exito-en-1440.png).
- [Éxito 390 px A++](.artifacts/1793654221037-exito-390-a++.png) y
  [320 px A++](.artifacts/1793654221037-exito-320-a++.png), alto contraste oscuro.
- [Panel autenticado 320 px](.artifacts/1793654221037-admin-320.png): recepción
  confirmada; UUID se parte en líneas y documento mantiene ancho 320 px.
- [Error Offline](.artifacts/1793653936511-error-sin-conexion.png).

Móvil público: documentWidth=390/320 según viewport, sin desbordamiento. Panel
320 px: documentWidth=320; las pestañas fuera de vista pertenecen al menú con
desplazamiento horizontal, no ensanchan el documento. La captura móvil anterior de
«Cargando…» y el header en mitad del fullpage por foco/scroll NO son evidencia final.

La comprobación visual no equivale a certificación WCAG completa. Propuesta de
pulido, sin implementar: [PROPUESTA_PULIDO_VISUAL.md](../PROPUESTA_PULIDO_VISUAL.md).

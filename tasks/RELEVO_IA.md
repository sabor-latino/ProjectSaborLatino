# Relevo de IA — Sabor Latino

> ESTADO VIGENTE: implementación manual ya autorizada y realizada en C:/Users/ownne/source/repos/SaborLatino-whatsapp (feature/solicitudes-whatsapp-manual). 81 backend/23 frontend y hooks correctos; e2e Chrome aprobado en demo, con capturas. Las secciones históricas que dicen «solo planificación/no implementado» quedan superadas por este estado. Consultar tasks/IMPLEMENTACION_WHATSAPP.md del worktree activo y su checklist. No reimplementar ni reactivar WhatsApp API automática. Pendiente publicar producción solo con alcance autorizado y aplicar previamente esquema/baseline revisados; cierre documental/propuesta estética al final del relevo.

Última actualización: 2026-11-02. Este archivo conserva contexto para otra sesión/modelo, no transfiere permisos ni herramientas automáticamente. Instrucciones actuales del usuario y reglas del entorno tienen precedencia. Verificar el estado real antes de actuar.

## Arranque obligatorio de quien retoma

1. Leer AGENTS.md y CONTRIBUTING.md en la raíz; después este archivo, donde lo dejamos.md, tasks/todo.md, la decisión final de tasks/plan.md y COMUNICACION_CODEX_GEMINI.md si existe. Seguir sus límites vigentes.
2. Ejecutar git status --short, git branch --show-current y git rev-parse HEAD. No asumir que la rama local coincide con main publicado. No hacer pull/reset/clean, cambio de rama destructivo ni limpiar artefactos ajenos.
3. Identificar en tasks/todo.md la primera tarea pendiente del ALCANCE ACTUAL. Las casillas de planes históricos descartados no son trabajo autorizado.
4. Comprobar herramientas, skills, credenciales y subagentes realmente disponibles. No afirmar que se heredan sesiones de este chat ni que una prueba vieja acredita el estado actual.
5. Informar al usuario brevemente de lo retomado y continuar solo el alcance autorizado. Una petición de plan/relevo no inicia por sí sola implementación ni despliegue.

## Decisión vigente del usuario: WhatsApp MANUAL

El usuario eligió expresamente la versión manual después de explorar y descartar una alternativa automática.

- El negocio usa la aplicación WhatsApp Business.
- Número receptor confirmado por usuario: +506 8526 2096; enlace base https://wa.me/50685262096.
- El cliente llena los datos del evento y pulsa Solicitar cotización por WhatsApp.
- Primero se guarda la solicitud en el backend/panel y se obtiene una referencia.
- Solo tras guardar se abre WhatsApp con resumen y referencia codificados. El cliente pulsa Enviar manualmente.
- La conversación y la preparación de la cotización siguen manualmente.
- Al recibir el mensaje, personal autorizado podrá marcar manualmente «Recibido por WhatsApp» en el panel. No afirmar que esa acción ya existe.
- Registro conservado aunque cliente cierre WhatsApp. Error al guardar no abre chat; doble clic/reintento/reapertura no duplican la solicitud.
- Abrir enlace NO acredita envío ni entrega. La confirmación manual se distingue de estados comerciales/aceptación de contrato.
- Sin API WhatsApp Business Platform, proveedor, webhook, bot, recepción automática ni mensajes automáticos. No reactivar WA-1..WA-7 ni A-1..A-6.

## Plan pendiente, aún NO implementado

1. Cerrar campos de contacto: usuario rechaza depender del correo para gestionar por WhatsApp; correo opcional y teléfono obligatorio/opcional siguen por definir. No usar correo ficticio ni tocar el correo Identity de inicio de sesión.
2. Adaptar persistencia/validaciones e idempotencia para guardar solicitud antes de navegar. Preservar registros antiguos y autenticación existente.
3. Construir resumen ES/EN de nombre, evento, fecha/hora, lugar, invitados, paquete/promoción y comentarios aplicables, más referencia. No inventar precio o disponibilidad confirmados.
4. Botón y textos claros; navegación móvil/escritorio tras guardado, alternativa reabrir/copiar que reutilice solicitud. Evitar ventana emergente nueva tras await que el navegador bloquee.
5. Acción administrativa de recepción manual con permisos y fecha/autor.
6. Pruebas significativas, builds, navegador y auditoría independiente antes de entrega. En esta fase solo se editaron documentos: no afirmar implementación ni pruebas de función ejecutadas.

Mejora visual posterior acordada: footer sin newsletter con tres columnas (marca, navegación, contacto/redes) y franja de cotización. Ya existe banda final en Inicio y footer tres columnas en main: integrar sin duplicar. Redes/horarios/datos no confirmados se omiten, no se inventan.

## Mapa de documentos y fuentes

- tasks/todo.md: checklist activo resumido.
- tasks/plan.md: historial y decisión final manual. Las decisiones posteriores explícitas sustituyen propuestas anteriores.
- tasks/plan-whatsapp.md: PROPUESTA AUTOMÁTICA DESCARTADA, conservada solo como historial. No usar como plan activo.
- donde lo dejamos.md: bitácora cronológica y pruebas anteriores, no garantía del presente.
- Fuente oficial click-to-chat: https://faq.whatsapp.com/5913398998672934.

## Código que revisar al implementar

En una copia actualizada y aislada de main, leer:
- frontendSL/src/paginas/SolicitarCotizacion.tsx.
- frontendSL/src/paginas/panel/Solicitudes.tsx y contratos api/tipos.ts.
- backendSL/DTOs/Solicitudes/SolicitudDtos.cs, Models/Solicitud.cs y Services/SolicitudService.cs.
- Servicios/controladores de cotización y pruebas existentes antes de cambiar estados.
- Layout.tsx, Inicio.tsx e index.css solo al abordar footer.

En main revisado: correo de solicitud obligatorio y teléfono opcional; .Trim() de correo no admite null. No se encontró envío automático de email ni generador PDF: los textos no son evidencia de funcionalidad. Cotización puede tener estado comercial Enviada sin transporte real. Confirmar todo contra revisión actual.

## Estado de Git y despliegues observado; revalidar

Raíz: C:/Users/ownne/source/repos/ProjectSaborLatino-backend.
Último estado local observado: rama feature/backend-resiliencia-catalogo-eventos, HEAD 6e3e717de0b02e37e2ed9b0cd186ad035d8d5eed. Rama backend antigua respecto de main publicado: no programar frontend sobre esa base sin preparar copia/branch correcta.

Cambios documentales de esta conversación: tasks/plan.md; nuevos tasks/todo.md, tasks/plan-whatsapp.md y este relevo. donde lo dejamos.md es local y puede estar ignorado. dist-admin/ y dist-public/ aparecían sin seguimiento desde antes de este trabajo de diseño: no borrar ni incorporar a commits.

Publicación previamente comprobada del commit 2d8dab3e485a05077320e396b58702c1a8abb7af:
- https://sabor-latino-seven.vercel.app
- https://sabor-latino-admin.vercel.app
- API https://saborlatino-api-8dde8f3e.azurewebsites.net

Ambos proyectos Vercel quedaron conectados al repositorio GitHub con producción main y compilaciones separadas. Azure se publicó manualmente y se preparó workflow automático en PR #18: https://github.com/sabor-latino/ProjectSaborLatino/pull/18. El usuario decidió DEJAR SU FUSIÓN A SUS COMPAÑEROS. No fusionarlo; consultar estado si resulta pertinente, no asumir que sigue abierto ni que primera ejecución OIDC fue verificada.

Los directorios temporales usados en sesiones anteriores no son una base duradera ni fuente de verdad. Revalidar fuente main, CI y acceso autorizado; no recuperar credenciales en el texto de relevo.

## Reglas y skills que se conservan

- Regla crucial del usuario: utilizar el máximo número útil de subagentes para agilizar tareas independientes, dentro de los límites reales. Leer `.cursor/rules/subagentes-paralelismo.mdc`. El principal conserva coordinación y revisión; cada agente hereda reglas, skills, alcance y decisiones. Evitar edición simultánea de archivos y compartir pruebas/BD sin aislamiento. No duplicar encargos ni fingir delegación.
- Español; humor ligero y respetuoso cuando encaje; autonomía dentro del alcance autorizado.
- Aplicar las skills correspondientes a cada fase y reglas AGENTS; el usuario reiteró que son obligatorias. Leer SKILL.md antes de usarlas por primera vez en la nueva sesión. Usar subagentes especializados para implementación/auditoría según disponibilidad; no simular trabajo delegado.
- Planificación/context-engineering para continuidad; diseño API y seguridad para contratos; TDD y cambios incrementales para lógica; frontend y navegador para UI; revisión independiente, Git, CI, documentación y shipping al entregar.
- Nunca tocar la base PERSONAL LocalDB SaborLatinoDb. Pruebas solo SaborLatinoDb_Test_<GuidN> con limpieza; demo solo SaborLatinoLiveDemoDb. Azure producción tiene un nombre coincidente: distinguir entorno y alcance antes de cualquier migración.
- Sin secretos en Git, chat, logs o relevo. Sin mensajes reales a contactos durante pruebas salvo instrucción concreta para piloto.
- Verificaciones del proyecto: backend dotnet test backendSL.Tests/ProjectSaborLatino.Tests.csproj; frontend lint, test:ci, build:public, build:admin y verificador CI; hooks pre-commit/pre-push y navegador conforme AGENTS. Solo marcar comprobado lo ejecutado en la revisión pertinente.

## Cómo entregar el siguiente relevo

Antes de un corte previsto o al cerrar un incremento:
1. Actualizar este archivo, tasks/todo.md y donde lo dejamos.md con decisión vigente, estado Git, archivos propios/ajenos y siguiente paso concreto.
2. Anotar comandos/pruebas con resultado, revisión y momento; distinguir no ejecutado, fallido y aprobado. No copiar logs completos ni secretos.
3. Documentar procesos propios que quedan activos y evidencias necesarias; nunca asumir que siguen disponibles en la sesión siguiente.
4. Dejar decisiones abiertas y autorizaciones/acciones expresamente reservadas al usuario/equipo.
5. Revisar consistencia entre los tres archivos y ejecutar git status --short. No publicar/confirmar cambios ajenos para fabricar un punto de control.

Si el corte es abrupto antes de actualizar: leer primero la última decisión manual y estado real; resolver incertidumbre con inspección o pregunta concreta, sin tratar como completado lo que no tiene evidencia.

## Prompt corto para pegar en otra IA

> Continúa Sabor Latino en C:/Users/ownne/source/repos/ProjectSaborLatino-backend. Lee primero AGENTS.md y tasks/RELEVO_IA.md; después los documentos indicados allí. Conserva reglas, skills obligatorias y subagentes. El plan vigente es MANUAL: guardar solicitud en panel, abrir WhatsApp +50685262096 con resumen/referencia, cliente pulsa Enviar y negocio marca recibido manualmente. NO implementar API/webhooks/bot. El cambio aún está en planificación; confirma el alcance solicitado antes de programar. Revisa estado Git real, protege cambios ajenos y nunca toques la base personal LocalDB. La fusión PR18 se reservó a compañeros. Retoma la primera tarea pendiente vigente y actualiza el relevo con evidencia.

## Instrucciones adicionales del usuario — cierre obligatorio

Al terminar implementación manual, PROPONER un pulido visual del frontend (títulos, botones, jerarquía, espaciado y estética), aplicando skills frontend/accesibilidad. No rediseñar arquitectura ni implementar la propuesta visual sin revisar alcance. Usar capturas del sitio real como evidencia.

Realizar pruebas rigurosas, añadir regresiones cuando corresponda y verificar de extremo a extremo con consola/red y capturas EN VIVO. Reactivar el flujo de pruebas navegador antes fallido: formulario -> guardado -> salida WhatsApp controlada -> panel -> marcar recepción manual. No se reactiva la API automática descartada.

Estado de implementación: worktree C:/Users/ownne/source/repos/SaborLatino-whatsapp, feature/solicitudes-whatsapp-manual, base5428206. Backend/frontend implementados por subagentes, pruebas en curso. Subagentes alcanzaron límite de uso; principal retoma integración. Herramientas Chrome MCP aisladas preparadas en tasks/qa-whatsapp. No dar como terminado hasta comprobar evidencias. Original permanece intacto para código. La instrucción actual autoriza implementar; el texto histórico de planificación queda superado.

## Estado actual de pruebas — implementación manual

Implementación autorizada y en curso en worktree SaborLatino-whatsapp, main5428206. El frontend pasó20/20, lint y builds en precommit EXIT0; backend primer pase79/80, falla lista explícita migraciones8vs9. Backend actualiza lista conservando rigor y repite suite80. QA independiente detectó exposición de evolución privada por replay de creación: corregida respuesta segura y regresión. Frontend añade recovery tras retorno/refresh y cobertura TTL/actor/almacenamiento denegado. Chrome MCP e2e preparado, aún pendiente ejecutar tras inicio demo.

Usuario exige propuesta posterior de pulido visual (títulos/botones/jerarquía) y pruebas exhaustivas con capturas; subagentes reactivados tras rehabilitar límites. No afirmar completado ni desplegar producción antes de la verificación.

## Resultado implementación manual — 2026-11-02

Función implementada LOCALMENTE en worktree C:/Users/ownne/source/repos/SaborLatino-whatsapp, rama feature/solicitudes-whatsapp-manual base5428206. No se publicaron estos cambios ni se hizo commit/push/merge.

Verificación:81/81backend y23/23frontend; lint/tipos/build genérico/public/admin, verificador separación26/35módulos y hooks precommit/prepush EXIT0. EF sin cambios pendientes;0bases temporales residuales; migración9 aplicada solo SaborLatinoLiveDemoDb. Identity/base personal intactos.

E2E Chrome real: tasks/qa-whatsapp/.artifacts/1793653992461-integration.json passed con solicitud artificial#3 (guardar201, URL/resumen correcto, retorno nueva navegación/reload mismoID, ES/EN, login panel y marca manual manteniendo Pendiente). Offline separado1793653936511-integration.json passed: falla conserva formulario y no abre chat. Capturas móvil originales en carga fueron descartadas; visual1793654158216-visual.json passed rehidrata únicamente datos artificiales#3 para revisión sin otroPOST ni credenciales. Admin320 y móvil390/320A++/alto contraste revisados visualmente. Registro/auditoría de WhatsApp no afirma envío real. No mensajes enviados al negocio.

Demo ACTIVA: API http://localhost:5239 PID9652; público http://localhost:54528/solicitar PID6932; admin http://localhost:54529/ PID15584. Chrome visible con perfil separado. Logs y PIDs en tasks/demo-processes.local (ignorado) y TEMP/sabor-whatsapp-demo-f158033bce4944bd88f7975157bc0bbc. Servidores originales en puertos estándar se conservaron.

Límites: producción necesita migración9 revisada/aplicada y luego actualizar baseline del workflow antes de desplegar; no hacerlo por inferencia. PR18 ya estaba fusionado por compañeros en la base usada. Propuesta visual y reporte final de Chrome a cargo QA en tareas; fuente de función congelada, sin nuevos cambios después de pruebas finales.

## Cierre local verificado — WhatsApp manual

Implementación y QA completas, sin Required pendientes.81/81backend y23/23frontend; lint/tipos/builds y ambos hooks0; modelo sin pendientes y0bases temporales. Demo9migraciones, sin acceso a base personal ni publicación de esta función.

Informe completo: C:/Users/ownne/source/repos/SaborLatino-whatsapp/tasks/qa-whatsapp/RESULTADO.md. E2E real guardó solicitud#3 por UI y confirmó recepción manual en panel; retorno/reload mismoID. Offline probado aparte. Smoke limpio final: público48/admin53 peticiones;0errores+warnings consola,0fallos red,0pendientes. Capturas finales1793654221037 con contenido real/scroll0 revisadas, móvil390/320A++ y admin320 sin overflow. Las capturas previas en carga/scroll no son evidencia final.

Propuesta de pulido (SIN implementar): tasks/PROPUESTA_PULIDO_VISUAL.md. Priorizar un solo primario en éxito, texto «Guardar y abrir WhatsApp», etiquetas legibles, títulos/espaciado y autor administrativo legible; conservar A++/contrastes. Medición controlada no reprodujo huecoextra0px: no cambiar layout global por una captura inicial.

Siguiente paso usuario/equipo: revisar demo y propuesta; si se pide publicar, preparar migración9 Azure y actualización baseline después de verificar esquema. No implementar API automática ni fusionar/publicar por inferencia. No commits/push/merge en esta tarea. Aplicación pública http://localhost:54528/solicitar y admin http://localhost:54529 siguen activos; API5239 exclusivamente demo. Chrome visible abierto. Otra IA debe leer este cierre antes de cualquier texto histórico anterior de planificación.

## Propuesta visual ampliada — no implementada

Usuario pidió ampliar las mejoras visuales. tasks/PROPUESTA_PULIDO_VISUAL.md ahora desarrolla12áreas: títulos/textos, botones, formulario, confirmación, espaciado/superficies, navegación, portada, paquetes, galería, panel, footer y accesibilidad/estados. Revisiones paralelas frontend/QA/panel. Orden P1acciones/textos observados, P2consistencia/panel/footer, P3rutas adicionales tras capturas específicas. Mantiene identidad, A++/contrastes y flujo manual; distingue propuestas de datos/función (nombre autor necesita contrato). No se modificó código ni ejecutaron suites nuevas: solo documentación. Implementación funcional previa continúa completa y pruebas81/23 vigentes para aquella revisión.

## Estado actual: pulido visual AUTORIZADO y en ejecución

La orden posterior del usuario supera la etiqueta histórica de propuesta no implementada: ejecutar mejoras críticas primero y sencillas al final; comparar con páginas de internet conservando reglas. Agentes backend/frontend/QA respondieron y se reactivaron. Trabajo activo en C:/Users/ownne/source/repos/SaborLatino-whatsapp, sin commit/push/merge ni producción. Principal lleva documentación/referencias, frontend es único editor aplicación, QA navegador, backend revisor independiente. Consultar última sección tasks/plan.md para checklist y evidencia vigente;81/23 anteriores no acreditan la revisión visual nueva hasta repetir puertas pertinentes.

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

## Despliegue automático — 2026-11-02

- [x] Publicar main fusionado en Vercel público/admin y Azure; comprobar HTTP y CORS.
- [x] Conectar ambos proyectos Vercel a GitHub, producción main y variables persistidas.
- [x] Preparar y auditar workflow Azure OIDC y guard SQL; PR #18 abierto.
- [ ] Fusionar PR #18 con orden expresa y comprobar primera ejecución Azure automática.


Cierre: usuario reservó la fusión de PR #18 para sus compañeros. CI backend/frontend success y ambos previews Git Vercel READY. No fusionar sin instrucción nueva. Azure automático pendiente de esa fusión; publicación actual completa y Vercel automático conectado.


## Plan de implementación — Footer público y contacto por WhatsApp (2026-11-02)

Estado: propuesta en definición con el usuario. Esta petición autoriza redactar el plan y explorar ideas; no iniciar implementación, publicación ni fusión. La fusión del PR #18 sigue reservada a los compañeros.

### Objetivo y decisiones acordadas

Crear una franja «¿Armamos tu próximo evento?» con botón principal «Cotizar por WhatsApp», seguida de un footer compacto de tres columnas. Sin newsletter, formulario de suscripción ni captura de correo en el footer.

1. Marca: logo Sabor Latino y frase breve sobre música para eventos.
2. Explorar: Inicio, Paquetes, Galería y Preguntas frecuentes, reutilizando rutas existentes.
3. Contacto y redes: WhatsApp como contacto principal y perfiles oficiales confirmados. Horario/cobertura solo si el usuario aporta información real.

Propuesta visual inicial, aún ajustable: fondo oscuro ciruela coherente con el sitio, tipografía actual y acentos coral; WhatsApp reconocible por icono y texto. En móvil las columnas se apilan y el botón tiene ancho cómodo. Banda legal discreta con año automático.

### Evidencia y límites

- La versión main publicada ya tiene footer de tres columnas; se mejora sobre esa base. La rama local backend mantiene una versión antigua: no copiarla ni cambiar de rama sobre cambios ajenos.
- Inicio ya contiene una banda final de cotización con foto y textos editables ES/EN. La nueva franja debe integrarse con ella; no apilar dos bandas de conversión consecutivas ni perder silenciosamente la edición administrativa de esos textos.
- En el código main revisado no se encontraron teléfono WhatsApp ni perfiles oficiales. Su confirmación es necesaria antes de activar enlaces; no usar números ficticios, enlaces # ni horarios inventados.
- Ámbito funcional: footer del portal público. El administrativo conserva un cierre sobrio y su aislamiento.
- Esta propuesta no solicita cambios del formulario actual de cotización ni elimina su correo: el rechazo del usuario se refiere a la suscripción del footer. No ampliar ese alcance sin definirlo.
- No hacen falta cambios de API, migraciones ni base de datos para esta primera versión. El origen de los datos de contacto se decidirá al implementar, reutilizando configuración existente si la hay y evitando un CMS nuevo sin necesidad.

### FOOTER-1 — Definir contenido y boceto

Dependencias: ninguna. Alcance: diseño/documentación.

Aceptación:
- [ ] Confirmar WhatsApp internacional, perfiles sociales y texto de marca; omitir datos no disponibles.
- [ ] Elegir tratamiento de franja y variante móvil con una única invitación final por página.
- [ ] Resolver la integración con la banda editable de Inicio y acordar textos ES/EN.

Verificación: revisar boceto escritorio/móvil con el usuario y registrar decisiones. No implementar todavía.

### FOOTER-2 — Implementar invitación y enlace WhatsApp

Dependencia: FOOTER-1. Alcance previsto: Layout o componente público pequeño, integración Inicio y estilos asociados (máximo 3–5 archivos).

Aceptación:
- [ ] Botón abre WhatsApp del negocio con saludo breve traducido y correctamente codificado; no envía mensajes automáticamente ni incorpora datos personales de formularios.
- [ ] Una única franja final en Inicio; otras páginas públicas siguen el patrón acordado. Conservar acceso al formulario existente de cotización.
- [ ] La franja no aparece en el portal administrativo ni introduce enlaces de login/registro en el público.

Verificación: prueba funcional del número/destino, ES/EN y ausencia de duplicación; navegar con teclado. No enviar un mensaje real durante las pruebas.

### FOOTER-3 — Actualizar columnas y estilos adaptables

Dependencia: FOOTER-1 y FOOTER-2. Alcance previsto: Layout, index.css y textos de preferencias o componentes equivalentes (máximo 3–5 archivos).

Aceptación:
- [ ] Tres columnas en escritorio y orden vertical legible en móvil; sin newsletter ni campos de correo.
- [ ] Solo rutas existentes y redes confirmadas, iconos con nombre accesible y enlaces externos seguros; año automático y marca sin traducir.
- [ ] Sin desbordamiento a 320 px ni al ampliar texto al 200%; foco visible, controles cómodos y contraste suficiente en los modos actuales.

Verificación: inspección visual a 320, 768 y 1440 px; modo claro/oscuro y alto contraste; texto ampliado y recorrido de teclado.

### FOOTER-4 — Validación y entrega

Dependencias: FOOTER-2 y FOOTER-3. Alcance: pruebas enfocadas y documentación, con auditoría de subagente independiente.

Aceptación:
- [ ] Pruebas de WhatsApp, traducciones, navegación y aislamiento público/admin aprobadas; lint y builds public/admin sin errores.
- [ ] Verificación real de consola/red y capturas escritorio/móvil; revisión independiente sin defectos bloqueantes.
- [ ] Actualizar seguimiento y presentar cambio revisable; respetar las reglas de PR y no fusionar sin orden expresa.

Verificación: al implementar, ejecutar pruebas frontend pertinentes, verificador CI y puertas/hook del repositorio. Si se ejecuta suite backend por reglas del proyecto, usar exclusivamente bases temporales y confirmar limpieza. No se ejecutan suites por esta modificación únicamente documental.

### Preguntas abiertas y siguientes ideas

- ¿La franja será sobria con color de marca o usará una foto real de una presentación? No añadir carrusel ni vídeo sin decidirlo.
- Número WhatsApp y redes oficiales. Horario y zona de cobertura, si se desea mostrarlos.
- Boceto siguiente sugerido para definir: portada con una foto protagonista y dos acciones claras (ver paquetes / cotizar); permanece fuera del alcance aprobado hasta conversarlo.
- Otras ideas para revisar después, no comprometidas: tarjetas de servicios por tipo de evento, galería de presentaciones reales y testimonios verificables. No inventar reseñas, estadísticas ni garantías.

Tareas resumidas en tasks/todo.md. Auditoría de planificación sabor_qa completada: detectó banda de Inicio existente y confirmó criterios móvil, accesibilidad, ES/EN y contacto real.

## Revisión de alcance — Cotizaciones mediante API de WhatsApp (2026-11-02)

Prioridad: definir e integrar el flujo real de solicitudes/cotizaciones. El usuario confirmó AMBAS direcciones: recibir avisos de solicitudes en el WhatsApp del negocio y enviar la cotización al WhatsApp del cliente. Seguimos en definición del plan; no enviar mensajes, contratar servicios ni publicar esta integración todavía.

Esta aclaración SUSTITUYE la suposición del plan de footer de que bastaría un enlace wa.me y de que no habría cambios de API/base de datos. El footer de tres columnas sin newsletter sigue válido como mejora visual, pero queda subordinado al flujo real. Un enlace de chat puede ser secundario; no cuenta como integración API terminada.

### Hechos comprobados en main fusionado

- SolicitarCotizacion promete envío por correo y exige correo de contacto; teléfono es opcional.
- SolicitudService guarda la solicitud. No se encontró un servicio de envío de correo en los servicios/controladores revisados.
- Crear cotización asigna EstadoCotizacion.Enviada aunque no hay transporte de mensajes. Separar estado comercial y estado de entrega; no presentar aceptación de API como entrega al destinatario.
- No se encontró una función PDF existente; el envío PDF queda como opción a definir, no como función disponible.
- Azure App Service F1 puede suspender el proceso. Un BackgroundService dentro de ese proceso no garantiza procesar mensajes cuando duerme.

### Flujo propuesto

1. Cliente completa formulario existente con datos del evento, teléfono WhatsApp internacional y consentimiento de contacto. Propuesta: correo de contacto opcional, pendiente de confirmar; correo de acceso Identity no cambia.
2. API guarda solicitud y aviso pendiente de manera atómica. La confirmación web distingue solicitud registrada de notificación enviada o entregada.
3. Servicio WhatsApp notifica a personal del negocio autorizado con referencia y resumen mínimo de la solicitud. Definir número emisor API y número receptor del personal: no asumir que el emisor puede enviarse avisos a sí mismo. Si el negocio quiere atender desde su aplicación actual, verificar coexistencia admitida para esa cuenta antes de registrar/migrar el número.
4. Personal prepara/revisa la cotización en panel y elige Enviar por WhatsApp. Se conserva control humano sobre montos, condiciones y vigencia; no añadir bot que calcule precios.
5. API envía resumen y forma segura de consultar detalle al cliente. Registrar identificador de proveedor y estados pendiente/aceptado por proveedor/entregado/leído (si se informa)/fallido.
6. Webhooks autenticados actualizan entrega y, si se acuerda, incorporan respuestas a la conversación asociada. Un texto recibido no acepta contratos/cotizaciones ni cambia permisos automáticamente.

### Tareas en orden

- [ ] WA-1 — Confirmar cuenta/número actual, emisor y destinatario interno, administrador Meta, país y presupuesto. Evaluar integración directa oficial WhatsApp Business Platform; elegir proveedor solo con requisitos y costes actuales comprobados. No solicitar tokens por el chat.
- [ ] WA-2 — Definir contrato: teléfono y consentimiento, tratamiento opcional del correo, solicitudes antiguas sin teléfono, textos ES/EN y canal de respuesta. Criterio: formulario/API/modelo coherentes; Identity y solicitudes históricas preservados.
- [ ] WA-3 — Persistencia y transporte: solicitud + outbox en misma transacción, servicio de proveedor aislado, reintentos acotados e idempotencia por evento/versión. Criterio: fallo/timeout no pierde solicitud, no duplica mensajes y es recuperable desde panel. Resolver procesamiento duradero compatible con F1; no depender únicamente de proceso web despierto. Aprobar disponibilidad/coste del mecanismo antes de aprovisionarlo.
- [ ] WA-4 — Aviso al negocio: destinatario autorizado distinto de emisor si se requiere envío separado, mensaje mínimo y enlace al panel autenticado. Criterio: creación genera aviso correcto en entorno de pruebas sin exponer datos a destinatarios arbitrarios.
- [ ] WA-5 — Envío al cliente desde panel: autorización Admin/Superadmin, destinatario verificado/apto, cotización vigente/versionada y estado de entrega independiente del comercial. Criterio: envío explícito registra intento/ID y distingue pendiente de entregado; reenvíos no duplican por accidente. PDF o consulta por enlace seguro se define antes de implementar documentos.
- [ ] WA-6 — Webhooks: desafío de verificación y validación de firma según proveedor, eventos duplicados y fuera de orden, minimización de logs y seguimiento de fallos. Criterio: eventos falsos se rechazan y una entrega repetida no duplica acciones.
- [ ] WA-7 — Pruebas y salida: proveedor simulado, pruebas negativas y de reintentos/duplicados, aislamiento backend temporal y QA frontend/público/admin, hooks y navegador. Después piloto con números de prueba autorizados, despliegue y comprobación real de estados antes de activar en producción.

### Requisitos de operación a resolver

- Guardar credenciales solo en configuración segura backend de Azure; nada en Vite, repositorio ni logs. Configurar cuenta Meta y callback HTTPS real cuando el usuario autorice activación.
- Consentimiento de contacto, baja y protección frente a solicitudes abusivas; crear solicitud web no equivale a iniciar una conversación de WhatsApp.
- Verificar reglas vigentes de sesión, mensajes iniciados por empresa, plantillas o Direct Send disponible para esa cuenta. No asumir que todo mensaje podrá ser texto libre ni prometer gratuidad o elegibilidad.
- Autenticación o autorización limitada para enlaces a cotización; no exponer detalles por IDs secuenciales ni permitir aceptación mediante enlace de lectura.
- Migraciones aditivas revisadas, bases temporales de pruebas y base demo local. No tocar base personal. Cambios de esquema requieren aplicar/revisar Azure y actualizar baseline del pipeline; PR #18 y su fusión siguen a cargo de compañeros.

Fuentes consultadas: https://whatsappbusiness.com/products/business-platform/ y https://www.twilio.com/docs/whatsapp/api (documentación oficial de plataforma y de proveedor; Twilio no elegido). La documentación detallada directa de Meta devolvió 429; confirmar especificación vigente de cuenta al implementar, sin basarse en SDK Node archivado.

Decisión siguiente: identificar si el número del negocio usa WhatsApp normal, WhatsApp Business o una cuenta API ya activada. No se requiere compartir credenciales para responder.

### Flujos obligatorios para WhatsApp — confirmación del usuario

El usuario reiteró que el uso de skills es obligatorio. Registrar evidencia de aplicación, no solo sus nombres:

- using-agent-skills: seleccionar flujos según la fase, declarar supuestos y mantener alcance.
- planning-and-task-breakdown: plan WA-1..WA-7 con dependencias y criterios de aceptación.
- api-and-interface-design: contratos de solicitud/envío/webhook, estados de entrega e idempotencia separados del estado comercial.
- security-and-hardening: credenciales solo backend, consentimiento, destinatarios válidos, firma de webhook y permisos de envío/reenvío.
- source-driven-development: especificación vigente del proveedor y cuenta confirmada antes de implementar; no usar SDK archivado.
- test-driven-development + incremental-implementation: al implementar, prueba que falle por cada comportamiento, incremento mínimo y regresión verde; no fingir RED/GREEN durante documentación.
- frontend-ui-engineering + browser-testing-with-devtools: al modificar formulario/panel, validar ES/EN, accesibilidad y flujo real en navegador.
- code-review-and-quality + doubt-driven-development: auditoría independiente de decisiones/código antes de aprobar; documentar incertidumbres sin atribuir revisiones inexistentes.
- git-workflow-and-versioning + ci-cd-and-automation + shipping-and-launch: cambios aislados, CI, esquema Azure revisado y activación con evidencia. No fusionar PR18; usuario reservó esa decisión.
- documentation-and-adrs + observability-and-instrumentation: seguimiento actualizado y estados/errores de envío observables sin revelar datos sensibles.

Revisión de alcance backend ya delegada a sabor_backend y recibida. Cuenta/tipo de número WhatsApp sigue pendiente de respuesta; se puede continuar definición sin enviar mensajes ni aprovisionar servicios.

## ALCANCE VIGENTE — Formulario → WhatsApp Business con mensaje preparado (2026-11-02)

Esta aclaración del usuario SUSTITUYE y deja FUERA DE ALCANCE WA-1..WA-7 (API, outbox, webhooks y envíos automáticos). El negocio ya tiene la aplicación WhatsApp Business. Quiere que el cliente pulse solicitar cotización, se abra su chat con los datos preparados y el cliente decida pulsar Enviar. Después la conversación sigue manualmente en WhatsApp. La anterior respuesta «ambas» no autoriza implementar mensajería API tras esta corrección explícita.

### Flujo y contenido

1. Conservar el formulario de datos del evento: nombre, tipo de evento, fecha/hora, lugar, invitados, paquete y comentarios según correspondan. Datos opcionales no deben producir líneas vacías o undefined.
2. Validar los datos necesarios y preparar un resumen ES/EN legible, incluyendo promoción seleccionada como solicitud, sin presentar descuento/precio ni disponibilidad como confirmados.
3. Botón «Solicitar cotización por WhatsApp» abre https://wa.me/NUMERO_NEGOCIO?text=TEXTO_CODIFICADO. Número internacional confirmado; texto con codificación de URL, conservando saltos de línea y caracteres especiales.
4. El cliente revisa y pulsa Enviar dentro de WhatsApp. En escritorio puede aparecer WhatsApp Web o un paso de acceso; no prometer apertura inmediata si falta aplicación/sesión.
5. El negocio recibe el mensaje del cliente y continúa la cotización manualmente desde su WhatsApp Business actual.

### Decisiones y pendientes

- Número oficial de WhatsApp Business con código de país: solicitado al usuario, pendiente. No usar número ficticio ni contratar/migrar cuenta.
- Para este canal no es necesario exigir correo del cliente; propuesta quitar obligatoriedad/campo del flujo WhatsApp sin modificar correo Identity ni contratos históricos backend.
- Guardar adicionalmente la solicitud en panel NO fue confirmado por el usuario. No enviar silenciosamente POST existente con correo ficticio. Separar «abrir chat» de «registrar solicitud»; si se pide conservar registro, definir esa persistencia y adaptar contratos explícitamente antes de implementar.
- La web no puede confirmar mediante este enlace si el cliente pulsó Enviar o si el mensaje llegó. Textos web deben decir que se abre WhatsApp para enviar, nunca «solicitud enviada/entregada» solo por redirigir.
- Esta fase continúa la definición solicitada, sin código/despliegue ni envío real de mensajes.

### Implementación prevista

- [ ] CHAT-1: confirmar número receptor y cerrar datos obligatorios; resolver si el usuario quiere además registro en panel.
- [ ] CHAT-2: construir resumen y enlace desde datos validados. Abrir directamente desde la interacción del usuario; evitar apertura de ventanas tras una espera asíncrona que pueda ser bloqueada. No poner credenciales/datos ocultos del perfil en el mensaje.
- [ ] CHAT-3: adaptar botón, texto explicativo y correo del formulario ES/EN. Mantener el formulario utilizable si se vuelve de WhatsApp; alternativa visible de copiar resumen si la navegación no se completa. No intentar detectar envío/entrega.
- [ ] CHAT-4: pruebas del enlace/número, caracteres &/#/acentos/saltos, campos opcionales, validación, ES/EN y regresión de cotización formal si se conserva. Verificar móvil/escritorio, teclado, sin envío real de prueba a negocio y sin enlaces admin públicos.
- [ ] CHAT-5: revisión independiente, pruebas frontend/builds y verificaciones del proyecto que apliquen, actualización de seguimiento y entrega revisable. PR18 sigue reservado a compañeros.

El footer sin newsletter sigue siendo propuesta visual separada; su CTA puede llevar al formulario para preparar el mensaje. No sustituir la recopilación de datos por un saludo vacío si el visitante ya llenó el formulario.

Skills aplicables al alcance vigente: planificación y documentación para esta fase; source-driven-development valida click-to-chat oficial; security-and-hardening limita datos del enlace; TDD, frontend-ui-engineering, browser-testing-with-devtools y revisión independiente al implementar. API/outbox/webhooks no se aplican como implementación porque fueron descartados por la aclaración.

Fuente oficial verificada: https://faq.whatsapp.com/5913398998672934 (click-to-chat en móvil/WhatsApp Web, número internacional y mensaje prellenado codificado). No se requiere integrar WhatsApp Business Platform para este flujo.

### Número receptor confirmado por el usuario

WhatsApp Business: +506 8526 2096. Destino click-to-chat: https://wa.me/50685262096, con parámetro text codificado a partir del formulario. CHAT-1: número confirmado; sigue pendiente decidir si además se guarda la solicitud en panel y cerrar campos requeridos. Confirmado por el usuario, no comprobado mediante un envío real.

## DECISIÓN VIGENTE — Guardar en panel antes de abrir WhatsApp

Usuario confirmó: «Guardar solicitud también en el panel». Queda resuelta esa pregunta de CHAT-1; se conserva el número receptor +506 8526 2096. Esta sección completa el alcance vigente click-to-chat y sustituye la indicación previa de que el registro no estaba confirmado.

### Flujo acordado

1. Cliente completa y valida los datos del evento.
2. El backend guarda la solicitud, que aparece en el panel administrativo con su referencia y estado comercial pendiente.
3. Solo después de confirmar el guardado se abre WhatsApp con el resumen y el número de referencia de la solicitud.
4. Cliente pulsa Enviar en WhatsApp y la conversación continúa manualmente con el negocio.

Criterios de aceptación:
- Si falla guardar, no abrir el chat; conservar el formulario y mostrar error recuperable.
- Evitar duplicados al doble clic y reintentos: bloqueo del botón y clave de idempotencia por intento lógico validada en backend; un reintento de resultado incierto debe recuperar la misma solicitud. Reabrir WhatsApp reutiliza el registro existente.
- Incluir referencia de solicitud en el mensaje. No alterar aceptación, montos ni estados comerciales por abrir WhatsApp.
- Confirmación honesta: «Solicitud guardada. Complete el envío en WhatsApp». No indicar enviada/entregada: el sitio no puede saber si el cliente pulsó Enviar.
- Navegación automática tras guardar compatible con restricciones del navegador. Evitar window.open nuevo después de await; valorar navegación de la misma pestaña y ofrecer enlace explícito de continuación si el navegador no completa apertura. Probar el retorno/reapertura sin crear registros extra.
- Propuesta de contacto pendiente de cerrar: correo no obligatorio para este flujo; no asumir ni capturar el número WhatsApp del cliente mediante wa.me. Decidir si teléfono de contacto será requerido o voluntario. Si ambos se omiten y el cliente no envía el mensaje, el panel conserva el evento pero no permite contactarlo; no inventar contacto.

Impacto previsto: reutilizar POST de solicitudes y autorización del panel. Adaptar validaciones frontend/backend y nulabilidad/serialización de correo si se confirma opcional, sin tocar identidad de inicio de sesión; migraciones y protección de Azure conforme reglas del proyecto. Idempotencia con pruebas de concurrencia. Ninguna integración Meta API, webhook, outbox ni cuenta de pago.

Orden actualizado de CHAT-1..CHAT-5: cerrar campos de contacto; probar e implementar guardado/idempotencia; generar mensaje con referencia; adaptar formulario ES/EN y apertura WhatsApp; verificar flujo completo público→API→panel→WhatsApp con proveedor externo limitado a abrir chat, sin enviar mensaje real de prueba al negocio.

Revisión de planificación sabor_backend recibida: guardado previo, contacto real, idempotencia y confirmación de estado. Seguimos en planificación según petición original; no hay cambios funcionales ni despliegue de esta mejora.

## Propuesta actual para revisión: confirmación automática de WhatsApp

Usuario pidió ver el plan tras consultar viabilidad de confirmar automáticamente recepción. Documento consolidado: tasks/plan-whatsapp.md (A-1..A-6). Conserva guardado panel y envío manual cliente, añade webhook entrante y conciliación. Es PROPUESTA, no autoriza implementar ni reactivar el antiguo plan de envíos automáticos WA-1..WA-7. Coexistencia/coste por comprobar. Plan click-to-chat sigue como base hasta elegir opción. PR18 reservado a compañeros.

## DECISIÓN FINAL VIGENTE — Versión manual

El usuario eligió «dejar mejor la versión manual». Esta decisión prevalece sobre la propuesta automática A-1..A-6: no implementar webhook, integración WhatsApp Business Platform, proveedor ni confirmación automática. El documento plan-whatsapp.md se conserva solo como propuesta histórica descartada.

Flujo de implementación acordado para el plan:
1. Cliente completa datos del evento y pulsa Solicitar cotización por WhatsApp.
2. Backend guarda solicitud en panel y devuelve referencia. Si falla, conservar datos, mostrar error y no abrir WhatsApp.
3. Abrir chat https://wa.me/50685262096 con resumen y referencia codificados.
4. Cliente pulsa Enviar dentro de WhatsApp y la conversación continúa manualmente.
5. Al recibir el mensaje, personal autorizado puede marcar manualmente «Recibido por WhatsApp» en la solicitud; distinguir esa confirmación de los estados comerciales y de un envío verificado automáticamente.

Criterios: registro conservado aunque no se envíe mensaje, reapertura sin duplicar solicitud, referencia incluida y estado honesto. No registrar envío/entrega por el simple clic. Correo no obligatorio para canal WhatsApp es propuesta pendiente de cerrar junto con contacto telefónico. No alterar correo de acceso Identity. Pruebas futuras enfocadas en guardado, duplicados, URL/mensaje, permisos de confirmación manual, ES/EN y navegador.

La versión manual aún requiere implementar formulario/guardado/estado y sus pruebas; esta decisión solo actualiza el plan. Sigue vigente la reserva del usuario: la fusión del PR #18 corresponde a sus compañeros.

## Continuidad entre sesiones e IA

Leer tasks/RELEVO_IA.md antes de retomar. Resume decisión manual vigente, reglas/skills, pendientes, estado observado y protocolo de cierre. Actualizar relevo + todo + bitácora antes de cortar o al cerrar cada incremento; revalidar Git/herramientas y no revivir propuestas descartadas. El relevo no transfiere permisos ni convierte planificación en implementación.

Regla crucial añadida por orden del usuario: máximo paralelismo útil con subagentes, heredando reglas/skills y sin ampliar alcance; responsables por archivo y aislamiento de pruebas. Persistida en AGENTS.md, .cursor/rules/subagentes-paralelismo.mdc y tasks/RELEVO_IA.md.

## Instrucciones adicionales del usuario — cierre obligatorio

Al terminar implementación manual, PROPONER un pulido visual del frontend (títulos, botones, jerarquía, espaciado y estética), aplicando skills frontend/accesibilidad. No rediseñar arquitectura ni implementar la propuesta visual sin revisar alcance. Usar capturas del sitio real como evidencia.

Realizar pruebas rigurosas, añadir regresiones cuando corresponda y verificar de extremo a extremo con consola/red y capturas EN VIVO. Reactivar el flujo de pruebas navegador antes fallido: formulario -> guardado -> salida WhatsApp controlada -> panel -> marcar recepción manual. No se reactiva la API automática descartada.

Estado de implementación: worktree C:/Users/ownne/source/repos/SaborLatino-whatsapp, feature/solicitudes-whatsapp-manual, base5428206. Backend/frontend implementados por subagentes, pruebas en curso. Subagentes alcanzaron límite de uso; principal retoma integración. Herramientas Chrome MCP aisladas preparadas en tasks/qa-whatsapp. No dar como terminado hasta comprobar evidencias. Original permanece intacto para código. La instrucción actual autoriza implementar; el texto histórico de planificación queda superado.

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

## Pulido visual autorizado — ejecución activa

El usuario autorizó implementar la propuesta ampliada, empezando por riesgos críticos del flujo y dejando los ajustes sencillos para el cierre. Pidió comparación con páginas de internet y restaurar agentes/flujo si hubo fallo. Los tres agentes existentes respondieron y se reactivaron; no fue necesario reemplazarlos.

Alcance: cambios visuales/copy/semántica de controles en el worktree SaborLatino-whatsapp. Conservar marca, preferencias, rutas públicas/privadas, permisos y flujo manual WhatsApp. No despliegue, merge ni migración de producción en esta tarea.

Responsables: sabor_frontend único editor frontend; sabor_qa captura baseline y verifica Chrome aislado, sin builds compartidos; sabor_backend revisión independiente sin editar código. Principal coordina, investiga referencias y ejecuta hooks completos tras congelar fuente. Baseline funcional previo81/23 y capturas finales previas sirven para P1, nuevas rutas se capturan antes de cambios P3.

Orden verificable:
- [ ] P1: formulario agrupado, errores/foco, CTA Guardar y abrir WhatsApp y confirmación con acciones jerarquizadas.
- [ ] P2: controles cómodos, títulos/espaciado, panel distingue registro web/recepción y preparar cotización sin prometer transporte.
- [ ] P3: navegación, portada, tarjetas/galería y footer según capturas específicas; sin contenido inventado.
- [ ] Comparación pública de internet con capturas/mediciones desktop/móvil y límites de interpretación.
- [ ] Chrome antes/después, matriz accesibilidad y E2E manual sin mensajes reales.
- [ ] Hooks/tests/lint/builds, revisión independiente, documentación y relevo final.

## Pulido visual: fuente congelada y primer gate

Implementación visual por incrementos completada por frontend: fieldsets/ayudas/acciones/errorfocus, referencia destacada/Nueva solicitud y foco al reiniciar; panel recepción separado/aviso enfocable y Preparar/Guardar cotización; precio completo sin clamp heredado; títulos/portada/targets44/footer/caption. Sin contratos, roles, transporte ni dependencias nuevas.

Causa del recorte de precio confirmada: selector genérico .paquete p aplicaba clamp de descripción también al párrafo .precio; reemplazado por clase específica de descripción y pie flexible. QA Chrome encontró otro Required a320A++: datetime-local mínimo intrínseco excedía3px. Corregido con min-inline-size:0/width/max-width dentro del formulario; validación real de campo aún pendiente QA.

Primera tanda hooks terminó EXIT0: backend81/81 y frontend24/24, lint/tipos/build genérico/public/admin y separación26/35módulos. Segundo gate frontend incluyó fixfecha. Registro TEMP/sabor-visual-hooks-df64fd5a33bc44fbb6c0be0bb6ea01c1.log. Falta cierre matrizChrome/capturas/E2E y repetirprecommit tras QA estable para acreditar todafuente final. Revisiónfresh independiente de fuente/fix sin Required estático; no equivale a auditoría visual.

## Reparación autorizada en ejecución — 2026-11-03

El usuario ordenó «Soluciona todos los errores». Worktree activo C:/Users/ownne/source/repos/SaborLatino-azure-fix, rama fix/azure-schema-baseline, base main161531e. Tres agentes nuevos sí responden: CI implementa script/12 regresiones Windows/Linux, SQL revisa artefactos/seguridad, QA hace smoke producción de solo lectura. Rectificación del reporte anterior: el blob remoto usa NUL correctamente; los escapes duplicados eran JSON. Defectos reales: orden Path Windows/POSIX y baseline8 ante fuente9.

Producción SQL acreditó8 migraciones exactas, correo obligatorio y ausencia de nuevas columnas/índice. Se aplicó migration9 con SQL revisado SHA256 C181C2CAA8AC1CA96C060571350B958F278AFC761C2C3F70F5941112817FBF4F, transacción/XACT_ABORT/TRY-CATCH/rollback y destino Azure validado. Solicitudes1→1, acceso temporal retirado; inspector posterior completo en curso antes de baseline9. Nunca tocar base personal LocalDB.

Hooks locales precommit/prepush EXIT0:81backend/24frontend,lint/tipos/builds y separación26/35; script12regresiones verdes. npm audit producción0;12moderadas solo toolingESLint/minimatch sin parche10 compatible, no introducir override11 o npmforce en esta reparación. Baseline Chrome público+login admin:5rutas/8vistas, consola/red/overflow limpios después del warmup SQL; sin crear solicitudes ni mensajes.

Copia del paquete API anterior capturada desde Kudu, hash D67AD3822FBFA4AF240821AA85ECCB7D1606A750D135F9B9060C0414F1B3CA4A en tasks/azure-repair/.artifacts; retención SQLPITR7días acreditada, no se ensayó restauración. Nuevo ZIP de appmain161531e preparado, todavía sin publicar. Falta SQLpostcheck, baseline9, PR/CI Linux y publicación/verificación. Preservar autorización explícita para merge según AGENTS al llegar a un PR concreto revisado.

## Publicación y fusión autorizada del PR20

El usuario autorizó explícitamente fusionar PR20 mediante respuesta a la pregunta de aprobación. ConectorGitHub fusionó head8b842a6 en main62e0d6a1c3eb978cffe447c39fa7480db17275df. CI de rama41647261461 y PR41647268262 pasaron, incluidos Windows/Linux12regresiones. No se pidió confirmación duplicada.

SQLpostcheck9 completo verde, índices/columnas correctos, solicitud conservada1→1 y reglas temporales retiradas. Publicación manual de la appmain161531e completada: Kudu286d55aa15af4e6ea311c1238bb426c1 status4; DLL remota SHA256 BF38626BEBCDA4A20E41D9A493013E601EC19F17E6BDF533E6A04A0BB32F1258 igual al paquete revisado. Postdeploy Chrome5rutas/8vistas, consola/red/overflow limpios, OPTIONS204ambosorígenes. Probes sin crear solicitudes: body{}400 con4errores requeridos y sinCorreoContacto, marker sinJWT401. El adaptador tuvo que decodificar UTF8 de byte[] antes de interpretar ProblemDetails; comprobación final estricta guardada.

El último paso pendiente es observar CI main41647996591 y el nuevo DeployAzure automático del SHA62e0d6a. No afirmar automatización completada hasta jobrealverde. Los6archivos publicados coinciden byteporbyte con localhashobj y origin/main. La rama local sigue basada161531e con patchya publicado medianteconector; no descartar cambioslocales/docs/artefactos por confundirlos con trabajo inédito. No hacer push de rama local sin reconciliar contexto.

Apertura visible de Chrome desde terminal fue rechazada por revisiónautomática (blocked by policy); CUAcreateBrowserTab devolvió Local browser use is disabled in Work settings. No se alteraron ajustes ni se sorteó la restricción. El smoke headless Chrome DevTools previamente autorizado sí generó6capturas reales y pasó. Informar este límite al cierre si sigue vigente.

## Cierre verificado — reparación Azure completada

PR20 fusionado con autorización expresa en main62e0d6a1c3eb978cffe447c39fa7480db17275df. CI main41647996591 success y Deploy41648425222 intento2 success (job125962164961). Todos los pasos reales verdes: SHA/CI, baseline SQL, publish, OIDC, webapps-deploy y HTTP/CORS. Kudu83679e11c78e4730a3232c7fc73e0dfc status4activecomplete.

El intento1 reveló otro bloqueo real que antes no se había ejecutado: AADSTS700213 porque GitHub desde julio2026 presenta subject con IDs inmutables. Se corrigió únicamente FICgithub-main de la identidad existente a repo:sabor-latino@343788676/ProjectSaborLatino@1389433606:ref:refs/heads/main. Issuer/audience/principal/rolWebsiteContributor appscope se conservaron; sin secretos ni roles nuevos. La hipótesis inicial de escapes duplicados se rectificó como representación JSON.

SQL9 completo verificado, solicitud existente1→1 y reglas temporales retiradas. QA final posterior al automático:5rutas/8vistas/6capturas,0errores/advertenciasconsola,0fallos/pendientesred,0overflow,GET200 yOPTIONS204ambosportales. No solicitudes válidas ni mensajes reales creados. Python12 y backend81/frontend24/linters/builds/hooks/CI verdes. Producción npm audit0,12avisosmoderados devtooling registrados separados. Informe completo tasks/azure-repair/RESULTADO.md, QA tasks/qa-azure/RESULTADO.md. Reparación de despliegue terminada; no repetir migración ni revivir el diagnóstico descartado.

Estado Git final: rama local fix/azure-repair-verified, HEAD62e0d6a igual a origin/main. Para conservar historia se archivaron solo los seis archivos propios ya publicados en un checkpoint Git antes del cambio; no se borró ni reaplicó ese checkpoint. Documentos/evidencias locales y cambios ajenos se preservaron. La advertencia histórica sobre patch local pendiente queda superada por esta sincronización. Ver tasks/azure-repair/.artifacts/local-checkpoint.json.

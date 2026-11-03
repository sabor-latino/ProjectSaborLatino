# Solicitudes con WhatsApp manual

## Flujo

La web registra primero la solicitud y devuelve referencia. Después prepara un enlace al WhatsApp Business +50685262096 con resumen ES/EN. El cliente pulsa Enviar en WhatsApp y el personal confirma la recepción manualmente desde el panel. El enlace no permite a la web saber si se envió un mensaje.

Formulario nuevo: teléfono de contacto requerido y correo opcional. La API conserva compatibilidad con consumidores anteriores que proporcionan correo válido sin teléfono; exige al menos un contacto válido. El correo Identity para login no cambia.

## API y datos

POST /api/solicitudes admite claveIdempotencia opcional Guid. Clave/payload normalizados y actor iguales devuelven la misma respuesta de creación201. Clave incompatible devuelve409. Índice único filtrado resuelve carreras entre procesos. Respuesta de creación/replay no contiene evolución privada de cotizaciones ni confirmación administrativa posterior.

POST /api/solicitudes/{id}/whatsapp-recibido exige Administrador o Superadministrador. Registra primer usuario/fechaUTC y es idempotente; no acepta la cotización ni modifica el estado comercial. DTO expone recibidoWhatsAppEn y recibidoWhatsAppPorUsuarioId a las consultas autorizadas. El panel evita enlaces mailto vacíos y muestra la confirmación manual.

Migración20261102190453_SolicitudWhatsAppManual: correo nullable, clave/huella idempotencia, fecha/usuario recepción y unique index. Aplicada solo SaborLatinoLiveDemoDb en esta tarea. No aplicar Down para revertir sin revisar datos. Producción aún no contiene este cambio; antes de publicar revisar/aplicar migración y actualizar baseline del workflow Azure únicamente tras confirmar esquema.

## Recuperación del navegador

Intento y resumen de esta solicitud se conservan hasta una hora en sessionStorage, aislados por actor y pestaña, sin tokens/perfil/cotizaciones. Reintentar un guardado incierto conserva clave; cambiar datos genera otra. Volver/recargar restaura el resumen sin otro POST ni apertura automática. Al pedir otra solicitud, expirar o cambiar actor se limpia la recuperación. Con storage bloqueado se conserva confirmación actual y se ofrece continuar manualmente.

## Demostración local

Workspace: C:/Users/ownne/source/repos/SaborLatino-whatsapp. Rama feature/solicitudes-whatsapp-manual, base5428206. Se conservaron servidores originales; demo nueva:

- Público http://localhost:54528/solicitar
- Admin http://localhost:54529/
- API http://localhost:5239

Inicio reproducible: ejecutar `tasks/start-whatsapp-demo.ps1 -ApiPort 5239 -PublicPort 54528 -AdminPort 54529 -AbrirChrome`. El script rechaza puertos ocupados y no detiene procesos ajenos. Solo usa base demo. PIDs/logs en tasks/demo-processes.local, ignorado por Git.

## Verificación

Hooks pre-commit y pre-push EXIT0;81/81 backend,23/23 frontend,lint,tipos y builds genérica/public/admin. EF sin cambios pendientes, consulta independiente cero bases temporales residuales. Nueva cobertura de contacto, replay/concurrencia/privacidad, permisos/recepciónUTC, navegación/reintento, recuperación/caducidad/storage bloqueado.

Herramientas y evidencia real de Chrome en tasks/qa-whatsapp; revisar su reporte final para integración y capturas. No se usaron credenciales de Antigravity: no había entradas MCP de navegador copiables y se reutilizó Chrome DevTools MCP oficial instalado. WhatsApp externo se bloquea deliberadamente durante QA: no se envían mensajes reales al negocio. Los fallos inducidos de transporte se registran separados del smoke limpio.

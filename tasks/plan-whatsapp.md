AVISO: PROPUESTA AUTOMÃTICA DESCARTADA. El usuario eligiÃ³ versiÃ³n manual; consultar decisiÃ³n final en tasks/plan.md y tareas vigentes en tasks/todo.md. No implementar este documento.

# Plan para revisiÃ³n â€” Solicitudes y confirmaciÃ³n automÃ¡tica por WhatsApp

Estado: PROPUESTA. El usuario pidiÃ³ ver el plan; no se autoriza todavÃ­a implementaciÃ³n, migraciÃ³n de nÃºmero, contrataciÃ³n, mensajes reales ni despliegue de esta funciÃ³n.

## Resultado esperado

El cliente llena el formulario, la solicitud queda guardada en el panel, se abre WhatsApp Business +506 8526 2096 con un mensaje preparado y el cliente pulsa Enviar. Cuando ese mensaje llega al negocio, el panel lo reconoce y muestra Â«Mensaje recibido por WhatsAppÂ». La conversaciÃ³n y la preparaciÃ³n de la cotizaciÃ³n siguen siendo humanas.

Esta propuesta amplÃ­a CHAT-1..CHAT-5 Ãºnicamente con recepciÃ³n automÃ¡tica y conciliaciÃ³n de mensajes. No retoma el plan antiguo de enviar cotizaciones automÃ¡ticamente, bots ni una cola de mensajes salientes.

## Experiencia de cliente y negocio

1. Formulario con nombre, evento, fecha/hora, lugar, invitados, paquete/promociÃ³n y comentarios pertinentes. Propuesta: correo opcional; telÃ©fono de contacto por confirmar segÃºn necesidad de seguimiento.
2. Al pulsar Solicitar, validar y guardar. Si hay error, conservar datos y no abrir WhatsApp.
3. Tras guardar, abrir chat con resumen y referencia de solicitud. Mantener opciÃ³n de reabrir/copiar sin crear otra solicitud.
4. Cliente revisa y envÃ­a en WhatsApp. Abrir chat por sÃ­ solo no confirma envÃ­o.
5. El proveedor notifica al backend del mensaje recibido. El sistema verifica origen, registra evento y relaciona referencia.
6. Panel muestra recepciÃ³n, hora y relaciÃ³n con la solicitud. Si falta referencia o no coincide de manera segura, pasa a revisiÃ³n manual sin asignarla por conjetura.

## Estados visibles

- Solicitud registrada: el backend confirmÃ³ persistencia.
- Apertura de WhatsApp iniciada: se lanzÃ³ el enlace; no confirma que la app abriÃ³ ni que el cliente enviÃ³.
- Mensaje recibido por WhatsApp: solo tras evento entrante autÃ©ntico relacionado con la solicitud.
- Pendiente de vincular: llegÃ³ mensaje sin referencia vÃ¡lida o con coincidencia ambigua.

Separar estos estados del estado comercial Pendiente/Cotizada/Aceptada y del estado de atenciÃ³n. Ausencia de evento no demuestra que el cliente no enviÃ³; puede existir demora o fallo de integraciÃ³n.

## A-1 â€” Validar cuenta e integraciÃ³n del nÃºmero actual

Dependencias: ninguna. Alcance: configuraciÃ³n/diseÃ±o, sin migrar ni contratar.

AceptaciÃ³n:
- [ ] Confirmar administrador, cuenta, propiedad del nÃºmero y opciÃ³n oficial compatible.
- [ ] Comprobar elegibilidad/coexistencia para conservar la aplicaciÃ³n WhatsApp Business. No darla por garantizada ni desregistrar el nÃºmero actual.
- [ ] Presentar requisitos, lÃ­mites y costes reales del proveedor elegido; fijar presupuesto y piloto antes de activar servicios.

VerificaciÃ³n: documentaciÃ³n oficial vigente y comprobaciÃ³n de cuenta con acceso autorizado. Si la opciÃ³n disponible afecta uso de la app o requiere nÃºmero distinto, informar y resolverlo antes de implementar. No comprar ni registrar cuentas por esta peticiÃ³n de plan.

## A-2 â€” Guardar solicitud y referencia de correlaciÃ³n

Dependencia: diseÃ±o A-1. Alcance: backend solicitud/DTO/modelo y pruebas en incrementos de hasta cinco archivos.

AceptaciÃ³n:
- [ ] Guardar la solicitud con una referencia opaca y Ãºnica para el mensaje; la referencia no otorga lectura, aceptaciÃ³n ni ediciÃ³n de la solicitud.
- [ ] Idempotencia al doble clic/reintento; reapertura WhatsApp reutiliza el mismo registro.
- [ ] Contacto opcional coherente entre formulario/DTO/base cuando se acuerde; sin correos ficticios ni cambios a Identity. Solicitudes histÃ³ricas preservadas.

VerificaciÃ³n: pruebas de validaciÃ³n, guardado, concurrencia, reintento incierto y acceso pÃºblico/admin. Usar Ãºnicamente bases temporales aisladas y confirmar limpieza. Revisar migraciÃ³n aditiva antes de Azure.

## A-3 â€” Formulario y apertura del chat

Dependencia: A-2. Alcance: formulario, utilidad de mensaje y pruebas/estilos pertinentes.

AceptaciÃ³n:
- [ ] Mensaje legible ES/EN, referencia y datos elegidos; saltos, acentos y sÃ­mbolos codificados correctamente.
- [ ] Abrir el chat solo tras guardar, sin depender de una ventana emergente nueva tras una espera; alternativa explÃ­cita de continuar/copiar.
- [ ] ConfirmaciÃ³n web honesta sobre registro/apertura; no prometer correo ni entrega. No presentar precio/promociÃ³n/disponibilidad como compromiso confirmado.

VerificaciÃ³n: destino +50685262096, validaciÃ³n, retorno/reapertura, teclado y mÃ³vil/escritorio sin enviar mensajes reales durante pruebas de interfaz.

## A-4 â€” RecepciÃ³n segura y persistente de mensajes

Dependencias: A-1 y A-2. Alcance: endpoint de webhook, recepciÃ³n persistente y pruebas por incrementos.

AceptaciÃ³n:
- [ ] VerificaciÃ³n del endpoint y autenticidad de notificaciones conforme proveedor; HTTPS y credenciales solo backend.
- [ ] Persistir evento de forma duradera antes de confirmarlo al proveedor; deduplicar por ID de mensaje/evento y procesar reintentos sin duplicar acciones.
- [ ] Vincular con referencia vÃ¡lida; ausencia/ediciÃ³n de referencia pasa a revisiÃ³n manual. TelÃ©fono o contenido parecido no bastan para una coincidencia ambigua.

VerificaciÃ³n: firmas falsas, repeticiÃ³n, orden alterado, referencia borrada/editada, caÃ­da temporal y recuperaciÃ³n. Validar recepciÃ³n con la infraestructura real: App Service F1 puede dormir; no confiar solo en memoria o en un proceso en segundo plano que desaparece. Evaluar receptor/procesamiento duradero alternativo Ãºnicamente si el piloto lo exige y tras revisar coste.

## A-5 â€” Estado y revisiÃ³n en panel

Dependencia: A-4. Alcance: DTO de estado, vista administrativa y pruebas.

AceptaciÃ³n:
- [ ] Panel muestra registro/recepciÃ³n/pendiente de vincular con fecha y referencia, sin confundir entrega ni estado comercial.
- [ ] Personal autorizado puede vincular casos ambiguos con rastro de auditorÃ­a y sin duplicar solicitud.
- [ ] Un mensaje recibido no acepta cotizaciones ni cambia contratos automÃ¡ticamente; datos del panel accesibles solo con autorizaciÃ³n existente.

VerificaciÃ³n: permisos, eventos repetidos, casos sin correlaciÃ³n y actualizaciÃ³n del panel mediante consulta periÃ³dica/recarga inicialmente; no introducir infraestructura de tiempo real sin necesidad.

## A-6 â€” Piloto, pruebas y publicaciÃ³n

Dependencias: A-1..A-5. Alcance: QA, configuraciÃ³n y documentaciÃ³n.

AceptaciÃ³n:
- [ ] SimulaciÃ³n del proveedor y pruebas de regresiÃ³n backend/frontend, lint, builds public/admin, verificador CI, hooks y auditorÃ­a independiente aprobados.
- [ ] Piloto con remitentes/receptores de prueba autorizados: guardar â†’ abrir â†’ envÃ­o manual â†’ webhook â†’ panel. Comprobar tambiÃ©n cliente que no envÃ­a y mensaje con referencia eliminada.
- [ ] Aplicar/revisar esquema de producciÃ³n, ajustar baseline del despliegue si cambian migraciones y habilitar gradualmente. Conservar alternativa de registro + chat si la recepciÃ³n automÃ¡tica falla.

VerificaciÃ³n: evidencias de mensajes/eventos reales solo dentro de piloto acordado, sin registrar secretos ni cuerpos completos de conversaciones en logs. Controles de errores y mÃ©tricas mÃ­nimas de recepciÃ³n/reintentos. No tocar LocalDB personal. FusiÃ³n de PR #18 permanece reservada a compaÃ±eros.

## Condiciones para considerar terminada la funciÃ³n

- Solicitud persiste aunque cliente cierre WhatsApp.
- Mensaje recibido vÃ¡lido marca su solicitud correcta; el doble evento no duplica.
- Mensaje sin referencia queda visible para revisiÃ³n, nunca se relaciona incorrectamente.
- Fallo del proveedor no pierde la solicitud y no produce una falsa confirmaciÃ³n.
- Uso del WhatsApp Business actual y coste de operaciÃ³n definidos y comprobados antes de activar.

## Skills y revisiÃ³n

PlanificaciÃ³n y desglose, diseÃ±o de API, seguridad y documentaciÃ³n oficial en esta fase; TDD e implementaciÃ³n incremental al cambiar cÃ³digo; frontend/accesibilidad y navegador para el formulario/panel; revisiÃ³n independiente, Git/CI y shipping para entrega. Subagente sabor_qa revisa esta propuesta. Toda comprobaciÃ³n futura se reportarÃ¡ como pendiente hasta ejecutarla.

## Fuentes y decisiones abiertas

- Click-to-chat: https://faq.whatsapp.com/5913398998672934
- Notificaciones de mensajes entrantes mediante webhook (documentaciÃ³n de proveedor, no elecciÃ³n de Twilio): https://www.twilio.com/docs/whatsapp/api#configuring-inbound-message-webhooks
- Plataforma oficial: https://whatsappbusiness.com/products/business-platform/

Pendientes antes de implementar: elegibilidad/coexistencia de cuenta actual, proveedor y presupuesto, campos de contacto definitivos y canal de acceso administrativo a Meta. No se han contratado servicios ni verificado esa elegibilidad todavÃ­a.

RevisiÃ³n sabor_qa completada: criterios de webhook autÃ©ntico y referencia, durabilidad/deduplicaciÃ³n y coexistencia previa incluidos. Una referencia correcta relaciona un mensaje; no verifica por sÃ­ sola identidad del remitente ni lo autoriza a ver/aceptar cotizaciones.

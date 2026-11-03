# Implementación en curso — WhatsApp manual

Autorizada por el usuario el 2026-11-02. Se usa worktree feature/solicitudes-whatsapp-manual en C:/Users/ownne/source/repos/SaborLatino-whatsapp, basado main5428206 (PR18 ya fusionado por compañeros). El checkout original conserva sus cambios sin modificarlos para el código.

## Contrato y decisiones

- POST /api/solicitudes guarda primero; claveIdempotencia Guid opcional mantiene compatibilidad con consumidores antiguos.
- CorreoContacto opcional; validar si viene. Backend exige algún contacto; formulario nuevo exige teléfono y permite correo opcional para seguimiento si el cliente no manda mensaje. Identity intacto.
- Doble clic/reintento con mismo payload/clave/actor devuelve mismo registro; reutilizar clave con otros datos/actor produce conflicto sin revelar información.
- WhatsApp receptor50685262096, mensaje codificado ES/EN con referencia y datos proporcionados. Cliente pulsa Enviar. No automatizar mensajes ni afirmar entrega por navegación.
- POST /api/solicitudes/{id}/whatsapp-recibido solo Admin/Superadmin, devuelve Solicitud. Primera fecha UTC y usuario se conservan; no altera estado comercial.
- DTO añade recibidoWhatsAppEn y recibidoWhatsAppPorUsuarioId, ambos nullable.
- Panel muestra correo solo si existe, recepción manual y evidencia de quién/cuándo.
- Footer sigue tres columnas y sin newsletter; CTA al formulario sin duplicar franja Inicio ni inventar redes.

## Trabajo paralelo

- sabor_backend: código/migración/pruebas backend; bin/obj backend exclusivos durante desarrollo.
- sabor_frontend: formulario/panel/footer/tipos y pruebas frontend; npm/node_modules/dist exclusivos durante desarrollo.
- sabor_qa: herramientas DevTools/Chrome y auditoría independiente, sin editar implementación.
- Principal: coordinación, revisión/integración, migración exclusivamente demo, puertas finales y documentación.

## Estado y límites

Implementación y pruebas aún en curso: no interpretar el contrato como funcionalidad terminada. Producción no se despliega ni cambia desde esta tarea local salvo instrucción posterior. No mutar SaborLatinoDb personal. Demo exclusivamente SaborLatinoLiveDemoDb. Ningún mensaje real al negocio durante QA.

CUA integrado comprobado: navegador deshabilitado. Usuario autorizó Chrome y reutilización de herramientas Antigravity; QA prepara DevTools oficial con perfil aislado, sin copiar credenciales/config privada. No modificar sesiones personales.

## Validación prevista

TDD por área; backend integración aislada; frontend lint/test/builds; verificador CI; hooks; EF sin cambios de modelo pendientes; cero bases temporales residuales. Navegador Chrome visible/aislado: crear solicitud y verificar panel/recepción manual, URL WhatsApp interceptada sin enviar, fallos de guardado, reapertura, móviles y ES/EN. Registrar consola/red y capturas. Auditoría final independiente.

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

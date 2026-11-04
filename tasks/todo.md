# Tareas — WhatsApp manual (implementación autorizada)

Workspace activo: C:/Users/ownne/source/repos/SaborLatino-whatsapp, feature/solicitudes-whatsapp-manual, base main5428206. Leer tasks/IMPLEMENTACION_WHATSAPP.md y tasks/RELEVO_IA.md. El plan automático sigue descartado.

- [x] Destino confirmado +50685262096; guardar panel antes de abrir chat.
- [x] Formulario: teléfono requerido, correo opcional; no cambiar Identity.
- [x] Backend: contacto válido, guardado idempotente y replay sin evolución administrativa privada.
- [x] Mensaje ES/EN con referencia; reapertura/copia sin otro POST.
- [x] Recuperación1h por pestaña/actor; caducidad/corrupción/storage bloqueado cubiertos.
- [x] Marcar recepción manual solo Admin/Superadmin, fechaUTC/primer autor sin tocar estado comercial.
- [x] Footer mantiene3 columnas, añade WhatsApp real y quita promesas email; sin newsletter/redes ficticias.
- [x] TDD y regresiones:81 backend,23frontend; lint/tipado/builds/CI y ambos hooks EXIT0.
- [x] EF sin cambios pendientes,9migraciones demo y0bases temporales residuales.
- [x] Chrome visible abierto con perfil aislado. Demo público54528, admin54529 yAPI5239 HTTP200/CORS204.
- [x] E2E Chrome, smoke limpio, capturas en vivo y revisión independiente sin Required pendientes.
- [x] Propuesta de pulido visual guardada en tasks/PROPUESTA_PULIDO_VISUAL.md, sin implementar.
- [x] Documentación y relevo actualizados con evidencias y límites de publicación.

No se publicaron estos cambios de función en producción, no commits/push/merge ni mensajes WhatsApp reales. PR18 ya figura fusionado por compañeros en base main usada. No tocar LocalDB personal ni detener servidores originales.

## Continuación visual autorizada
- [ ] Mejoras críticas de solicitud/confirmación y copy panel.
- [ ] Consistencia de títulos/controles y rutas públicas observadas.
- [ ] Comparativa internet y capturas antes/después.
- [ ] QA integral, hooks y revisión independiente sobre fuente final.
- [ ] Relevo actualizado con resultados reales y límites.

## Reparación Azure — completada
- [x] Verificar historia SQL y destino Azure explícito.
- [x] Aplicar únicamente delta8→9 con rollback ante error; verificar columnas/índice e integridad del registro existente.
- [x] Retirar todas las reglas de firewall temporales.
- [x] Corregir huella portable y actualizar baseline solo tras verificación SQL.
- [x] Añadir12regresiones y ejecutarlas en Windows/Linux;81backend/24frontend/builds/hooks/CI verdes.
- [x] Publicar API compatible y verificar binario y smoke.
- [x] PR20 fusionado con autorización expresa; main62e0d6a.
- [x] Corregir subject OIDC con IDs inmutables manteniendo permisos acotados.
- [x] Despliegue automático41648425222 intento2 success y QA posterior con capturas.
- [x] Relevo y reporte final guardados con límites: sin mensajes reales, ni solicitudes válidas creadas durante QA; avisos moderados devtooling separados.

# QA de producción, solo lectura

Ejecutar desde la raíz: `node tasks/qa-azure/production-readonly.mjs baseline`
o, después de publicar la revisión exacta, `node tasks/qa-azure/production-readonly.mjs postdeploy`.

Reutiliza el transporte local Chrome DevTools MCP1.12.0 con perfil temporal aislado,
sin estadísticas ni navegación a WhatsApp. Solo navega a cuatro rutas públicas
conocidas y a la raíz administrativa sin iniciar sesión. No envía formularios,
no pulsa botones, no lee credenciales/storage y no crea solicitudes.

La API se comprueba mediante GET de cuatro catálogos y CORS de ambos portales.
Los datos del catálogo no se guardan. Se esperan documentos, fuentes, imágenes
visibles y red; capturas de escritorio/móvil de paquetes, formulario y login.
DOM, accesibilidad, consola/red y capturas se guardan en `.artifacts/` ignorado.
No se utilizan scripts de integración de demo ni se ejecutan suites/builds.

Un resultado correcto acredita carga pública, contrato visible del formulario y
CORS. No acredita guardar una solicitud, idempotencia o recepción administrativa:
esos flujos requieren un piloto de escritura autorizado por separado.

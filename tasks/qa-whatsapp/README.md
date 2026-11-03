# QA local — WhatsApp manual

Herramientas auxiliares sin dependencias del frontend. Se usa Chrome DevTools MCP
oficial instalado localmente (comprobado: 1.12.0). El cliente stdio negocia MCP y
guarda los schemas reales antes de ejecutar acciones; no cambia configuración global.

## Comandos desde la raíz del worktree

```powershell
node tasks/qa-whatsapp/browser-qa.mjs schemas
# Solo después de que el principal confirme que estos servidores son la demo actual:
node tasks/qa-whatsapp/browser-qa.mjs smoke http://localhost:54526/
node tasks/qa-whatsapp/browser-qa.mjs smoke http://localhost:54527/ 'Portal Administrativo'
# Solo tras confirmar migración/conexión demo y servidores actuales:
node tasks/qa-whatsapp/integration-qa.mjs --demo-confirmada
# Fallo de red por separado: MCP no admite emulación Offline junto con blocklist.
node tasks/qa-whatsapp/integration-qa.mjs --demo-confirmada --offline-only
# Si los servidores originales ocupan 54526/54527, demo alternativa aislada:
$env:SABOR_QA_PUBLIC_URL = 'http://localhost:54528'
$env:SABOR_QA_ADMIN_URL = 'http://localhost:54529'
node tasks/qa-whatsapp/integration-qa.mjs --demo-confirmada
```

Si cambia la caché npm, definir `CHROME_DEVTOOLS_MCP_ENTRY` con la ruta al archivo
`chrome-devtools-mcp/build/src/bin/chrome-devtools-mcp.js`. No se descarga un paquete
distinto ni se instala nada en `frontendSL`. Node y Google Chrome deben estar instalados.

`schemas` inicia y cierra únicamente el proceso MCP; no abre Chrome. `smoke` abre
Chrome headless con perfil temporal aislado, toma DOM/accesibilidad, consola/red y
captura, y cierra su propio proceso. Nunca conecta con el perfil ni sesiones personales.
No inicia/detiene servidores de aplicación. Evidencia en `.artifacts/`, ignorada por Git.

`integration-qa.mjs` crea una solicitud artificial y confirma recepción manual desde
la UI administrativa de demostración. Prueba guardado, navegación externa bloqueada,
retorno/recarga, idioma, A++, contraste y vista móvil. El modo `--offline-only` usa
otro proceso sin blocklist, activa Offline antes del submit y no restaura la red ni
abre enlaces. No borra registros ni cambia el estado comercial. Usa exclusivamente
las credenciales demo documentadas; puede recibir la contraseña demo mediante
`SABOR_QA_DEMO_PASSWORD`. No captura cuerpos de login ni lee storage de autenticación.
Los resultados y capturas no se incorporan al repositorio automáticamente.
Solo se admiten orígenes HTTP localhost públicos 54526/54528 y administrativos
54527/54529. El principal confirma que apuntan a la demo vigente; los scripts no
detienen procesos originales. Si se usa el juego alternativo, el launcher debe
apuntar la API a 5239 y habilitar CORS de los puertos demo correspondientes.

Antigravity: se inspeccionaron solo entradas MCP de navegador en los archivos locales
`antigravity-cli/settings.json` y `antigravity/mcp_config.json`. No hay entradas
browser/chrome/puppeteer que reutilizar. Se emplea directamente el MCP Chrome ya
disponible; no se copiaron credenciales ni configuraciones de otros conectores.

## Seguridad del piloto local

- Servidor de demostración: `SaborLatinoLiveDemoDb`; nunca LocalDB personal.
- WhatsApp/wa.me están bloqueados en el navegador QA a nivel MCP. No enviar mensajes.
- Inspeccionar el href o interceptar salida, sin navegar al negocio real. Confirmar
  número `50685262096`, texto codificado y referencia; la URL no confirma envío.
- No leer cookies, tokens ni storage de autenticación. Login demo solo mediante UI.
- El humo solo observa. Las pruebas con escritura se hacen después de confirmación
  del principal y exclusivamente contra la demo, con datos identificados como QA.

## Checklist de integración pendiente

1. Crear por UI con teléfono, correo vacío y datos de evento válidos. Un POST termina
   antes de ofrecer WhatsApp; referencia y datos aparecen en panel.
2. Doble clic/reintento incierto conserva una solicitud. Error de guardado conserva
   formulario y no abre chat; reabrir/copiar no repite POST. Mismo identificador con
   payload cambiado no revela ni modifica una solicitud previa.
3. Mensaje ES/EN conserva acentos, saltos y símbolos, paquete/promoción solicitados y
   referencia; no inventa precio/disponibilidad. Correo de Identity no cambia.
4. Login administrativo demo: marcar recibido por WhatsApp conserva estado comercial,
   muestra primera fecha/autor y repetir no los sobrescribe. Anónimo/Cliente/Trabajador
   no pueden marcar. Verificar en API y UI según permisos del contrato.
5. Recarga/reapertura y validación accesible: teclado, foco en errores/éxito, móvil
   320/390 px, A++ y contraste, ES/EN, ausencia de login en portal público.
6. Registrar errores de consola y solicitudes fallidas reales. Las navegaciones
   WhatsApp bloqueadas deliberadamente se documentan aparte, nunca como fallo de API.
7. Footer: tres columnas, sin newsletter, sin redes inventadas ni duplicación de la
   banda final de Inicio; portal privado conserva su pie independiente.

El control nativo datetime-local requirió un adaptador de QA: setter nativo y eventos
input/change, autorizado por el principal, seguido de `checkValidity()` real. El
MCP dejaba ese campo vacío con `fill_form`. No es un cambio de la aplicación.

`visual-qa.mjs REPORTE-INTEGRATION.json` repite capturas sin POST de creación:
rehidrata únicamente el almacenamiento de la solicitud artificial de un E2E aprobado
y observa la UI real. Es evidencia visual, no una nueva prueba de persistencia. No
restaura perfiles ni tokens. Espera texto visible, fuentes/imágenes y hace scroll al
inicio antes de capturar para evitar fallback de carga o artefactos del header sticky.

Estado final y evidencia: [RESULTADO.md](RESULTADO.md). Las capturas móviles antiguas
que mostraban «Cargando…» quedan excluidas de la evidencia final.

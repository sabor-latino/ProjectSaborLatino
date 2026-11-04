# Baseline de producción — 2026-11-03

**Estado final: QA posterior al despliegue automático aprobado.** Ver última sección.

Estado: **aprobado, solo lectura**. No acredita todavía la corrección ni un despliegue nuevo.

Evidencia: `.artifacts/baseline-1793712092327.json`, obtenida con
`node tasks/qa-azure/production-readonly.mjs baseline`, exit0; sintaxis Node y
`git diff --check` correctos. Chrome DevTools MCP1.12.0 con perfil temporal aislado.

- Cuatro GET de catálogos públicos y GET galería con Origin administrativo:200 y CORS exacto; tiempos199–1043ms tras despertar.
- Sitio público: `/`, `/paquetes`, `/galeria`, `/solicitar`. Administración: raíz redirige a `/login` como está previsto, sin autenticarse.
- Ocho observaciones de escritorio1440/móvil390: cero errores/advertencias de consola, cero fallos de red, cero peticiones pendientes y cero overflow horizontal al terminar.
- Seis capturas desktop/móvil de paquetes, formulario y login. Revisados visualmente paquetes/formulario en ambos tamaños y login móvil; las capturas son de viewport, no de toda la página.
- Formulario completo inspeccionado por DOM/accesibilidad: teléfono requerido, correo opcional, etiquetas presentes y botón «Guardar y abrir WhatsApp». Sin enlaces administrativos en las rutas públicas observadas.
- No POST/PUT/PATCH/DELETE, submit, login, edición de formulario, creación de solicitudes, navegación WhatsApp ni mensajes reales. No lectura de credenciales/storage.

Antes del smoke final, cuatro GET públicos agotaron45s; el GET posterior de galería
administrativa respondió200 en9.8s y los siguientes pasaron. Es compatible con el
arranque frío ya descrito, sin afirmar una causa nueva a partir de estos tiempos.
El primer intento del harness se detuvo por tratar la redirección administrativa
prevista como inesperada; se declaró su destino exacto y se repitió la matriz completa.

## Después del despliegue

Repetir `node tasks/qa-azure/production-readonly.mjs postdeploy` después de que el
principal confirme publicación de la revisión exacta. Contrastar salud/CORS,
consola/red, formulario y capturas con este baseline. El principal verifica por
separado la historia SQL, índice/columnas, SHA y ejecución del workflow.

Guardar solicitud, idempotencia y marca de recepción administrativa quedan fuera
de esta comprobación de lectura; no se atribuyen como probados en producción.

## Postdeploy manual — aprobado

Tras confirmación del principal de API main161531e publicada y SQL9 verificado,
se ejecutó `node tasks/qa-azure/production-readonly.mjs postdeploy`: **EXIT0**.
Ventana:2026-11-03T13:46:41Z–13:47:12Z.
Evidencia: `.artifacts/postdeploy-1793713601405.json` y seis capturas con ese prefijo.

- La misma matriz de cinco rutas/ocho vistas1440/390 aprobó: cero errores/advertencias consola, cero fallos/peticiones pendientes de red, cero overflow horizontal; fuentes e imágenes visibles completas.
- Cuatro catálogos públicos y galería con Origin administrativo:200/CORS exacto; servicios4867ms y siguientes202–482ms.
- Comparación contra baseline: mismos destinos, títulos de contenido, campos y botones en las ocho vistas. Formulario con correo opcional/teléfono requerido y sin navegación administrativa pública.
- Las seis capturas desktop/móvil se inspeccionaron visualmente, sin regresión observada respecto del baseline.
- Preflight real OPTIONS `/api/galeria` a13:47:00–01UTC: ambos orígenes204 con allow-origin exacto, métodoPOST y headers authorization/content-type permitidos; no se envió POST.
- No login, envío de formulario, creación de solicitudes, navegación a WhatsApp ni mensajes. El principal prueba por separado validaciones API; esta evidencia sigue limitada a lectura.

No se acredita aquí ejecución automática OIDC: el despliegue confirmado fue manual.

## Postdeploy automático — aprobado, QA final

El principal confirmó PR20 fusionado en main62e0d6a,
[CI41647996591](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41647996591)
correcto y [Deploy41648425222, intento2](https://github.com/sabor-latino/ProjectSaborLatino/actions/runs/41648425222/attempts/2)
correcto, incluido Azurelogin, publicación y VerifyAPICORS. Esos estados se atribuyen
a la verificación del principal; este agente realiza la comprobación HTTP/navegador posterior.

Nueva ejecución independiente `node tasks/qa-azure/production-readonly.mjs postdeploy`:
**EXIT0**,2026-11-03T13:59:37Z–14:00:08Z. No sobrescribe los artefactos manuales:
reporte `.artifacts/postdeploy-1793714377345.json` y seis capturas con ese prefijo.

- Cinco rutas/ocho vistas1440/390 aprobadas: cero errores/advertencias de consola, cero fallos o peticiones pendientes de red, cero overflow horizontal; fuentes/imágenes visibles completas.
- Cinco GET de catálogo200/CORS exacto para los orígenes públicos y administrativo correspondientes. Servicios4183ms; siguientes198–471ms.
- Seis capturas desktop/móvil revisadas visualmente; ninguna regresión observada. Destinos, headings, campos y botones coinciden con el baseline en las ocho vistas.
- Formulario conserva correo opcional/teléfono requerido y CTA previsto; no enlaces administrativos en público. Raíz administrativa redirige al login conocido.
- OPTIONS `/api/galeria` a14:00:19UTC: ambos orígenes204, allow-origin exacto, POST y authorization/content-type permitidos. Solo se envió OPTIONS.
- Sin login, submit, POST/PUT/PATCH/DELETE, solicitudes nuevas, navegación WhatsApp ni mensajes reales. La creación/idempotencia/recepción administrativa no se afirman probadas por este smoke.

QA final cerrado sin hallazgos pendientes dentro de este alcance de lectura.

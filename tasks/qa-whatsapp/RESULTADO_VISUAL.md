# Resultado QA — Auditoría Visual y Accesibilidad — 2026-11-02

Chrome DevTools MCP oficial 1.12.0 por stdio, Google Chrome 156 headless en entorno
aislado con perfil temporal. Servidores locales activos de la demostración: público 54528,
admin 54529, API 5239; base de datos `SaborLatinoLiveDemoDb`. No se afectaron perfiles
personales ni se transmitieron mensajes reales por WhatsApp.

---

## 1. Cobertura de la Matriz Visual y Accesibilidad

Evidencia base: [.artifacts/1793678376877-accessibility.json](.artifacts/1793678376877-accessibility.json) (EXIT 0).
Se auditaron sistemáticamente **48 combinaciones** en rutas públicas (`/solicitar` y `/paquetes`):

- **Idiomas:** Español (ES) e Inglés (EN).
- **Modos de contraste:** Estándar, Alto contraste claro y Alto contraste oscuro.
- **Escala tipográfica:** Normal y A++ (Extra grande, +200%).
- **Viewports evaluados:** 1440×900 px (escritorio), 768×900 px (tablet), 390×844 px (móvil estándar) y 320×844 px (móvil estrecho).

### Métricas observables:
| Métrica auditada | Casos evaluados | Fallos detectados | Estado |
|---|---|---|---|
| Desbordamiento horizontal del documento (`documentWidth > width`) | 48 | 0 | **APROBADO** |
| Recorte o solapamiento de precios en tarjetas (`TarjetaPaquete`) | 48 | 0 | **APROBADO** |
| Asociación de etiquetas de formulario (`label` / `aria-labelledby`) | 48 | 0 | **APROBADO** |
| Tamaño de controles de revelación (`summary` / disclosure $\ge$ 44px) | 48 | 0 | **APROBADO** |
| Control nativo `datetime-local` en 320px A++ | 2 (ES/EN) | 0 | **APROBADO** |

### Correcciones visuales verificadas en vivo:
1. **Precios en paquetes:** Se desacopló la regla `-webkit-line-clamp` del contenedor genérico. La descripción del paquete cuenta con selector específico y el pie distribuye el importe de forma flexible. Los importes demo (₡54 000, ₡68 000, ₡153 000, ₡255 000) se visualizan completos y sin corte a 320 px y 390 px.
2. **Campo fecha en 320 px A++:** El control `input[type="datetime-local"]` respeta `min-inline-size: 0` y `max-width: 100%`, permaneciendo editable por teclado (`valid: true`) sin causar ensanchamiento del viewport.
3. **Navegación por teclado y foco accesible:**
   - Enlace de salto («Saltar al contenido») transfiere foco directo a `#contenido`.
   - Envío de formulario inválido enfoca el primer campo con error (`#nombre`).
   - Campo teléfono valida patrón numérico e informa `aria-invalid="true"` enlazado a `telefono-error`; al ingresar formato válido (`+506 8888 1111`) regresa a `valid: true`.

---

## 2. Inspección Visual de Rutas Públicas (Antes vs Después)

Evidencia: [before-1793677537990-routes.json](.artifacts/before-1793677537990-routes.json) vs [after-1793678266789-routes.json](.artifacts/after-1793678266789-routes.json).
Se verificaron capturas full-page con scrollY=0 tras esperar fuentes web y decodificación completa de imágenes:

| Ruta | Viewport | Document Width | Encabezados visibles | Imágenes cargadas |
|---|---|---|---|---|
| Inicio (`/`) | 1440 px | 1440 px | H1 + H2 presentes | Sí (todas completas) |
| Inicio (`/`) | 390 px | 390 px | H1 + H2 presentes | Sí |
| Paquetes (`/paquetes`) | 1440 px | 1440 px | H1 + H2 presentes | Sí |
| Paquetes (`/paquetes`) | 390 px | 390 px | H1 + H2 presentes | Sí |
| Galería (`/galeria`) | 1440 px | 1440 px | H1 + H2 presentes | Sí |
| Galería (`/galeria`) | 390 px | 390 px | H1 + H2 presentes | Sí |
| Solicitar (`/solicitar`) | 1440 px | 1440 px | H1 + H2 presentes | Sí |
| Solicitar (`/solicitar`) | 390 px | 390 px | H1 + H2 presentes | Sí |

---

## 3. Comprobación de Desbordamiento Horizontal en 320px y 390px

Evidencia: [.artifacts/1793678439470-visual.json](.artifacts/1793678439470-visual.json) y [.artifacts/1793678266788-integration.json](.artifacts/1793678266788-integration.json).

### Formulario y Confirmación de Solicitud Manual (Portal Público):
- **390 px A++ con Alto Contraste Oscuro:**
  - `documentWidth = 390 px` (ancho de ventana = 390 px).
  - 0 elementos desbordados.
  - Captura: [.artifacts/1793678439470-exito-390-a++.png](.artifacts/1793678439470-exito-390-a++.png).
- **320 px A++ con Alto Contraste Oscuro:**
  - `documentWidth = 320 px` (ancho de ventana = 320 px).
  - 0 elementos desbordados.
  - El texto de resumen para WhatsApp, títulos y botones de acción (`Reabrir WhatsApp`, `Copiar resumen`, `Nueva solicitud`) se adaptan sin recortarse ni provocar scroll horizontal.
  - Captura: [.artifacts/1793678439470-exito-320-a++.png](.artifacts/1793678439470-exito-320-a++.png).

### Portal Administrativo (`/panel/solicitudes`):
- **320 px Móvil:**
  - `documentWidth = 320 px` (ancho de documento exacto, sin barra de desplazamiento en la ventana).
  - El menú de navegación superior (`.panel__menu`) tiene configurado intencionalmente desplazamiento horizontal deslizable en móvil (`overflow-x: auto; white-space: nowrap; scrollbar-width: none`). Los enlaces de pestañas (`Promociones`, `Catálogo`, `Personal`, `Galería`, `Solicitudes`) fluyen internamente en su contenedor táctil sin ensanchar el documento ni romper el diseño responsive.
  - La tarjeta de solicitud con recepción confirmada se adapta íntegramente a 320 px; el identificador de usuario y la fecha de confirmación se parten en línea de manera legible.
  - Captura: [.artifacts/1793678439470-admin-320.png](.artifacts/1793678439470-admin-320.png).

---

## 4. Auditoría de Red, Consola y Fugas de Memoria

### Consola y Red:
- **Público (`/solicitar`):** 0 errores de consola, 0 advertencias, 0 peticiones pendientes.
- **Admin (`/panel/solicitudes`):** 0 errores de consola, 0 advertencias, 0 peticiones pendientes.
- **Transporte simulado:**
  - La navegación saliente hacia `https://wa.me/50685262096` es interceptada y bloqueada en el entorno de pruebas (`net::ERR_INTERNET_DISCONNECTED`), protegiendo de cualquier envío real accidental.
  - La prueba de fallo sin conexión (`--offline-only`) arrojó el error de red esperado de forma controlada sin desmontar el formulario ni perder datos.

### Fugas de Recursos y Memoria:
- **Almacenamiento (`sessionStorage`):**
  - Limitado a un TTL estricto de 1 hora (`VIGENCIA = 3600000 ms`).
  - Aislado por actor (`actor: "visitante"` vs credenciales de sesión). Si cambia el actor o expira la vigencia, se ejecuta purga automática.
  - Al presionar «Nueva solicitud», se invoca inmediatamente `limpiarSolicitudPendiente()`, evitando retención de datos residuales.
  - Validación de tamaño y esquema estricto (rechaza payloads mayores a 30 KB).
- **Ciclo de vida React y Event Listeners:**
  - Desmontaje limpio de componentes (`SolicitudDisponible`, `FormularioSolicitud`, `SolicitudEnviada`).
  - No existen temporizadores recurrentes (`setInterval`), suscripciones globales huérfanas ni referencias DOM desacopladas en closures.
  - El reinicio mediante botón «Nueva solicitud» incrementa la clave de estado (`setFormulario`), forzando recolección de basura del árbol anterior y reasignando el foco de forma limpia a `#nombre`.

---

## 5. Conclusión de QA Visual

La suite de aseguramiento de calidad visual y accesibilidad cumple satisfactoriamente con los estándares establecidos:
- **Layout responsive:** 0 desbordamientos horizontales en 320 px y 390 px en público y panel.
- **Accesibilidad:** 48/48 pruebas aprobadas en combinaciones extremas (A++, alto contraste, viewports móviles).
- **Integridad del flujo:** Flujo completo operativo de extremo a extremo sin errores de consola ni fugas de memoria.

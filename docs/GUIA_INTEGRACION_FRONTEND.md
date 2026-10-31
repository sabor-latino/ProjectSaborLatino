# Guía de Integración Frontend ↔ Backend — ProjectSaborLatino

Este documento reúne los **contratos vigentes del backend (`backendSL/`)** endurecidos entre las órdenes `ORDEN-004` y `ORDEN-015`, junto con un **diagnóstico técnico y correcciones recomendadas** para el código actual de `frontendSL/` (PRs #10 y #11).

> **Nota de alcance:** El equipo/agentes de backend no modifican archivos dentro de `frontendSL/`. Esta guía sirve como referencia exacta para que la persona responsable del frontend aplique los ajustes directamente en su rama/PR.

---

## 1. Contratos vigentes del Backend (`ORDEN-004` a `ORDEN-015`)

### 1.1. Cotización como visitante sin cuenta vs. usuario autenticado (`POST /api/solicitudes`)

- **Visitante sin cuenta (sin cabecera `Authorization`):**
  - `POST /api/solicitudes` permite crear una solicitud de cotización públicamente y responde `201 Created`.
  - La solicitud se guarda con `ClienteUsuarioId = null` y los datos de contacto suministrados en el formulario (`NombreCliente`, `TelefonoCliente`, `CorreoCliente`).
- **Cliente con sesión iniciada (con `Authorization: Bearer <token>`):**
  - `POST /api/solicitudes` asocia automáticamente la solicitud al usuario autenticado (`ClienteUsuarioId = usuario.Id`).
  - Solo las solicitudes con `ClienteUsuarioId == usuario.Id` aparecen luego en `GET /api/solicitudes/mias`.

### 1.2. Registro público único y no apropiación de historial anónimo (`ORDEN-004`, `ORDEN-011`)

- **Endpoint único de registro público:** `POST /api/cuenta/registro` (`201 Created`). Crea la cuenta en Identity con `Activo = true` y le asigna el rol `Cliente`.
- **Cierre de ruta genérica de Identity:** `POST /api/identity/register` está deshabilitado y devuelve siempre `404 Not Found` (y no aparece en OpenAPI/Swagger), evitando que se creen cuentas sin rol `Cliente`.
- **Sin vinculación retroactiva por correo (`ORDEN-011`):**
  - Registrar una cuenta con un `correo` **NO** reclama ni vincula solicitudes enviadas previamente sin sesión (`ClienteUsuarioId = null`) que hubieran escrito ese mismo correo, ya que el endpoint público no verifica propiedad del buzón de correo y permitirlo expondría datos personales e historial de terceros.
  - Si un cliente desea consultar y gestionar sus solicitudes/cotizaciones desde su panel (`GET /api/solicitudes/mias`), debe iniciar sesión **antes** de enviar la solicitud.

### 1.3. Validación en vivo de tokens Bearer por request (`ORDEN-006`)

En cada request autenticado con `Authorization: Bearer <token>`, el middleware posterior a `UseAuthentication()` valida contra la base de datos:

1. Que el usuario exista, tenga `Activo == true` y no esté bloqueado (`LockoutEnd > DateTimeOffset.UtcNow`).
2. Que el claim `AspNet.Identity.SecurityStamp` del token coincida con el `SecurityStamp` actual en base de datos.
   - Cuando un administrador desactiva o reactiva un usuario (`PATCH /api/usuarios/{id}/activo`) o cambia su rol (`PATCH /api/usuarios/{id}/rol`), el backend rota el `SecurityStamp`.
   - Cualquier token emitido antes de la desactivación queda invalidado de inmediato y recibe **`401 Unauthorized`** en el siguiente request.
3. Que los claims de rol en memoria se sincronicen en cada request con los roles vigentes en base de datos, aplicando cambios de permisos (`200 OK` o `403 Forbidden`) sin esperar a que el token expire.

### 1.4. Invariante `Empleado.UsuarioId <-> Rol Trabajador` (`ORDEN-007`)

- Un registro de `Empleado` solo puede estar vinculado (`UsuarioId != null`) a un usuario que exista, esté activo y posea el rol `Trabajador`.
- Si se intenta cambiar el rol de un usuario en `PATCH /api/usuarios/{id}/rol` a un rol distinto de `Trabajador` mientras ese usuario sigue vinculado a un `Empleado`, el backend responde **`400 Bad Request`**:
  > `"No se puede cambiar el rol del usuario porque está vinculado a un empleado. Desvincúlelo primero en Empleados."`
- **Flujo correcto en la UI:** primero editar el empleado en `PUT /api/empleados/{id}` enviando `usuarioId: null` (desvinculación explícita), y luego cambiar el rol del usuario en `PATCH /api/usuarios/{id}/rol`.

### 1.5. Solicitudes y Cotizaciones (`ORDEN-005`, `ORDEN-008`, `ORDEN-015`)

- **Paquete y servicio padre activos (`ORDEN-005`):**
  - `POST /api/solicitudes` (cuando incluye `paqueteId`) exige que tanto el `Paquete` como su `Servicio` padre estén activos (`Activo == true`). Si alguno está inactivo o no existe, devuelve **`400 Bad Request`**.
- **Serialización transaccional (`ORDEN-015`):**
  - Las operaciones `POST /api/cotizaciones`, `PATCH /api/solicitudes/{id}/estado`, `PATCH /api/cotizaciones/{id}/aceptar` y `PATCH /api/cotizaciones/{id}/rechazar` toman un bloqueo transaccional `UPDLOCK, HOLDLOCK` sobre la fila `Solicitud` en SQL Server, impidiendo condiciones de carrera (por ejemplo, aceptar dos cotizaciones en paralelo o aceptar una cotización mientras se cancela la solicitud).
- **Cierre en cascada al cancelar o rechazar solicitud (`ORDEN-008`):**
  - Cuando `PATCH /api/solicitudes/{id}/estado` pasa una solicitud a `Cancelada` o `Rechazada`, todas sus cotizaciones en estado `Enviada` pasan automáticamente a `Rechazada` en la misma transacción.
  - No se permite cambiar manualmente una solicitud a `Aceptada`, ni cancelar/rechazar una solicitud que ya esté `Aceptada` (`400 Bad Request`).
- **Rechazo de cotización (`ORDEN-008`):**
  - `PATCH /api/cotizaciones/{id}/rechazar` marca la cotización como `Rechazada`. Si era la última cotización `Enviada`, la solicitud **permanece en estado `Cotizada`** (no regresa a `Pendiente`), permitiendo emitir una nueva cotización o cerrar la solicitud.
- **Aceptación de cotización vencida (`ORDEN-008`, `ORDEN-014`):**
  - Si un cliente o administrador llama a `PATCH /api/cotizaciones/{id}/aceptar` y `FechaVencimiento.Date < DateTime.Today`, el endpoint devuelve **`400 Bad Request`** (`{ "mensaje": "La cotización está vencida." }`), **pero persiste en base de datos el cambio de estado `Enviada -> Vencida`** (junto con su registro en bitácora).

### 1.6. Eventos, Disponibilidad e Inventario (`ORDEN-009`, `ORDEN-013`)

- **Formato de fechas ISO locales sin `Z`:**
  - El backend almacena y compara fechas de eventos en hora local (`datetime2` sin zona horaria). Desde el frontend se deben enviar cadenas ISO locales `yyyy-MM-ddTHH:mm` (sin sufijo `Z` ni conversión `.toISOString()` que desplace la hora a UTC).
- **Disponibilidad por pico máximo simultáneo en intervalos semiabiertos `[desde, hasta)` (`ORDEN-013`):**
  - Tanto `GET /api/equipos/disponibilidad?desde=...&hasta=...` como las validaciones de reserva en eventos calculan el uso en el rango `[desde, hasta)` mediante el **pico máximo simultáneo** de eventos activos (`Estado != "Cancelado"`) que se solapen en un mismo instante, y **no** sumando ciegamente reservas que ocurren en momentos distintos dentro de la ventana.
  - Dos eventos contiguos donde el primero termina exactamente cuando empieza el segundo (`FechaFin == desde` o `FechaInicio == hasta`) **no** se solapan.
- **Creación de eventos y copia de equipos del paquete (`ORDEN-009`, `ORDEN-013`):**
  - En `POST /api/eventos`:
    - Si se envía `paqueteId`, el paquete y su servicio padre deben estar activos (`400 Bad Request` en caso contrario).
    - Si `copiarEquipoDelPaquete: true` y algún equipo incluido en el paquete está **inactivo** o **no tiene stock disponible suficiente** para el pico simultáneo del rango `[fechaInicio, fechaFin)`, la creación del evento **falla íntegramente con `400 Bad Request` y rollback transaccional** (no se crea un evento a medias).
    - Si el usuario desea crear el evento sin copiar automáticamente el equipo del paquete para asignarlo manualmente después, puede enviar `copiarEquipoDelPaquete: false`.
  - La creación de eventos con copia de paquete, el cambio de horario/reactivación en `PUT /api/eventos/{id}` y la asignación de equipos en `POST /api/eventos/{id}/equipos` se serializan en SQL Server con `sp_getapplock` (`Recurso = 'InventarioEquipos'`).
- **Protección de stock al editar un equipo (`ORDEN-009`, `ORDEN-013`):**
  - `PUT /api/equipos/{id}` rechaza con **`400 Bad Request`** reducir `CantidadTotal` por debajo del pico simultáneo reservado en eventos activos presentes o futuros (`FechaFin > DateTime.Now`).

### 1.7. Catálogo Inicial Sembrado: Paquetes, Precios Base e Inventario (`DbSeeder.cs`)

El catálogo sembrado al iniciar la aplicación (`GET /api/servicios`, `GET /api/paquetes`, `GET /api/equipos`) define **precios base** sujetos a las condiciones del evento (indicado explícitamente al inicio de cada descripción con `"Precio base."`).

| Servicio | Paquete | Precio Base (CRC) | Equipos incluidos | Notas de negocio |
|---|---|---|---|---|
| **Sonido** | `Sonorización para grupos o DJs` | `₡60 000` (`60000`) | 4× Parlante activo, 2× Subbajo, 1× Consola de sonido, 4× Monitor in-ear inalámbrico, 4× Luces de ambiente, 1× Máquina de humo | Cobertura hasta para 500 personas + técnico especializado de audio e iluminación. |
| **Karaoke** | `DJ Open Format` | `₡80 000` (`80000`) | 2× Parlante activo, 2× Subbajo, 2× Micrófono inalámbrico, 1× Pantalla para karaoke, 4× Luces de ambiente | Reemplaza al antiguo `"Karaoke bailable"`. El servicio de karaoke se puede incluir como un extra. |
| **Música en vivo** | `Grupo secuenciado Villa Guanacaste` | `₡170 000` (`170000`) | 2× Parlante activo, 2× Subbajo, 3× Micrófono inalámbrico, 1× Consola de sonido, 4× Luces de ambiente | Sonido adaptativo según el recinto e iluminación completa de escenario. |
| **Música en vivo** | `Marimba Orquesta Son del Gallo (en vivo)` | `₡300 000` (`300000`) | 4× Parlante activo, 2× Subbajo, 6× Micrófono inalámbrico, 1× Consola de sonido, 4× Monitor in-ear inalámbrico, 1× Pantalla para karaoke, 6× Luces de ambiente, 1× Máquina de humo | De 6 a 8 músicos en escena; incluye todos los equipos de los demás paquetes. |

- **Equipos sembrados (8 tipos):** `Parlante activo` (6), `Subbajo` (4), `Micrófono inalámbrico` (8), `Consola de sonido` (2), `Monitor in-ear inalámbrico` (6), `Pantalla para karaoke` (2), `Luces de ambiente` (8) y `Máquina de humo` (2). Todos cuentan con stock total suficiente para crear eventos con `copiarEquipoDelPaquete: true`.

---

## 2. Diagnóstico y Correcciones Recomendadas en el Frontend (`PRs #10 y #11`)

A continuación se detallan los hallazgos detectados al revisar la integración de `frontendSL/` frente a los contratos anteriores, con archivo, línea, causa y solución exacta recomendada.

### 2.1. Recursión infinita (`Maximum call stack size exceeded`) al fallar validación nativa de formularios

- **Archivo y línea:** `frontendSL/src/utilidades/formularios.ts:9`
- **Causa:**
  En el manejador `onInvalid`, se invoca `campo.checkValidity()` dentro del propio evento `invalid`:
  ```ts
  if (
     esCampoFormulario(campo) &&
    !campo.checkValidity() &&
    campo.getAttribute('aria-invalid') !== 'true'
  )
  ```
  En el DOM estándar, llamar a `campo.checkValidity()` sobre un control inválido **dispara sincrónicamente otro evento `invalid`**, que burbujea de nuevo al `form` y vuelve a ejecutar `manejarInvalidoFormulario`, provocando un desbordamiento de pila (`RangeError: Maximum call stack size exceeded`) cuando el usuario intenta enviar un formulario con campos vacíos o inválidos.
- **Solución exacta:**
  Reemplazar `!campo.checkValidity()` por la lectura de la propiedad pasiva `!campo.validity.valid` (que consulta el estado de validez sin disparar eventos):
  ```ts
  if (
    esCampoFormulario(campo) &&
    !campo.validity.valid &&
    campo.getAttribute('aria-invalid') !== 'true'
  ) {
    campo.setAttribute('aria-invalid', 'true')
  }
  ```

### 2.2. Rechazo de precios redondos en colones al crear una cotización

- **Archivo y línea:** `frontendSL/src/paginas/panel/Solicitudes.tsx:287`
- **Causa:**
  El campo numérico de monto está definido como:
  ```tsx
  <input id="c-monto" type="number" min={1} step="1000" value={monto} onChange={...} required />
  ```
  Según la especificación HTML para `<input type="number">`, la base del `step` es el atributo `min` (`1`). Por tanto, el navegador exige que `(valor - 1) % 1000 === 0`, aceptando valores como `1001` o `120001` y **rechazando como inválidos todos los montos redondos en miles de colones** como `120000` (`₡120 000`).
- **Solución exacta:**
  Cambiar `min={1}` a `min={1000}` (o bien usar `min={1} step="any"` / `step="1"` si se desean permitir montos exactos al colón):
  ```tsx
  <input id="c-monto" type="number" min={1000} step="1000" value={monto} onChange={(e) => setMonto(e.target.value)} required />
  ```

### 2.3. Sesión vencida silenciosa al enviar solicitud de cotización

- **Archivos y líneas:** `frontendSL/src/api/cliente.ts:76` y `frontendSL/src/paginas/SolicitarCotizacion.tsx:153`
- **Causa:**
  En `frontendSL/src/api/cliente.ts:76`, cuando el token guardado en `localStorage` ya superó su marca local `expira` (`Date.now() >= expira`), `obtenerToken()` devuelve `null` **sin limpiar la sesión ni emitir `EVENTO_SESION_EXPIRADA`**.
  Como `POST /api/solicitudes` acepta peticiones con o sin token, si el cliente tenía su sesión abierta en pantalla (`usuario !== null` en `SolicitarCotizacion.tsx`) pero el token acaba de expirar localmente, `apiFetch('/api/solicitudes', ...)` envía la solicitud **sin cabecera `Authorization`**, devolviendo `201 Created` con `ClienteUsuarioId = null`. El usuario ve en la UI que su sesión sigue iniciada, pero la solicitud se creó como visitante anónimo y no aparecerá en `"Mis solicitudes"`.
- **Solución recomendada:**
  1. En `obtenerToken()` (`frontendSL/src/api/cliente.ts:76`), cuando `Date.now() >= expira`, llamar a `limpiarSesion()` y disparar `window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA))` antes de retornar `null`.
  2. Opcionalmente, en `SolicitarCotizacion.tsx:153`, si `usuario` estaba presente al iniciar el envío pero ya no hay token vigente, avisar al usuario para que renueve su sesión antes de enviar la solicitud.

### 2.4. Recarga del listado tras `400 Bad Request` por cotización vencida

- **Archivo y línea:** `frontendSL/src/paginas/panel/Solicitudes.tsx:33` (`aceptarCotizacion`)
- **Causa:**
  Cuando `PATCH /api/cotizaciones/{id}/aceptar` responde `400 Bad Request` porque la cotización expiró, el backend **persiste el estado `Vencida`** en base de datos (`ORDEN-008`). Actualmente, el bloque `catch` en `Solicitudes.tsx:37` solo muestra el mensaje de error (`setError(...)`) sin recargar los datos, dejando el botón `"Aceptar"` visible y el estado `"Enviada"` en pantalla hasta que el usuario refresque manualmente.
- **Solución recomendada:**
  En `aceptarCotizacion` (`Solicitudes.tsx:33-40`), invocar `await recargar()` también dentro del `catch` (antes o después de `setError`) para que la UI refleje inmediatamente el estado `Vencida` persistido por el backend.

### 2.5. Cálculo de stock disponible al editar equipos de un evento y equipos desactivados

- **Archivo y línea:** `frontendSL/src/paginas/panel/Eventos.tsx:204`
- **Causa:**
  Al mostrar el máximo seleccionable de un equipo en un evento, la UI calcula:
  ```ts
  const disp = (d ? d.disponible : eq.cantidadTotal) + propio
  ```
  Donde `d.disponible` proviene de `GET /api/equipos/disponibilidad?desde=...&hasta=...` (que ya incluye a este mismo evento dentro del cálculo del pico simultáneo del intervalo) y `propio` es la cantidad ya asignada a este evento.
  - Si existen otros eventos solapados en subintervalos distintos del mismo rango, `d.disponible + propio` es una cota superior razonable en la UI, pero puede superar `eq.cantidadTotal` si en el instante de pico otro evento no solapaba con este; conviene acotarlo con `Math.min(eq.cantidadTotal, d.disponible + propio)`.
  - Además, si un equipo que ya estaba asignado al evento fue desactivado posteriormente, no aparecerá en `equipos` (`soloActivos=true`). El backend (`POST /api/eventos/{id}/equipos`) sigue validación estricta en servidor, pero acotar el máximo en UI evita confusiones visuales.
- **Solución recomendada:**
  ```ts
  const disp = Math.min(eq.cantidadTotal, (d ? d.disponible : eq.cantidadTotal) + propio)
  ```

### 2.6. Mensaje informativo en pantalla de Registro

- **Archivo y línea:** `frontendSL/src/paginas/Registro.tsx:59`
- **Causa:**
  El subtítulo dice `"Guarda tus solicitudes y revisa tus cotizaciones en cualquier momento."`. Dado que por seguridad (`ORDEN-011`) crear una cuenta no reclama solicitudes anónimas enviadas antes de registrarse, un usuario que primero cotizó sin cuenta y luego se registró podría pensar que verá esa solicitud anterior en su panel.
- **Solución recomendada:**
  Aclarar brevemente en el texto: `"Inicia sesión antes de cotizar para guardar tus solicitudes y revisar tus cotizaciones en cualquier momento."`

### 2.7. Contrato de CI para pruebas de Frontend (`verify-frontend-ci.mjs`)

- **Archivos:** `frontendSL/package.json` y `.github/scripts/verify-frontend-ci.mjs`
- **Requisito de CI (`ORDEN-010`):**
  El verificador `.github/scripts/verify-frontend-ci.mjs` ejecuta `npm ci`, `npm run lint`, `npm run build` y comprueba si existen archivos de prueba (`*.test.*`, `*.spec.*` o bajo `__tests__/`) dentro de `frontendSL/`.
  - **Importante:** Si el PR de frontend incluye archivos de prueba (por ejemplo `frontendSL/tests/*.test.mjs` como en el PR #11), **`frontendSL/package.json` debe definir obligatoriamente el script `"test:ci"`**:
    ```json
    "scripts": {
      "dev": "vite",
      "build": "tsc -b && vite build",
      "lint": "eslint .",
      "preview": "vite preview",
      "test:ci": "node --experimental-strip-types --test tests/*.test.mjs"
    }
    ```
  - Si existen archivos `*.test.*` pero falta `"test:ci"` en `frontendSL/package.json`, el job `Frontend (Lint, TypeScript + Vite Build & Test Contract)` en GitHub Actions fallará con código de salida `1`.

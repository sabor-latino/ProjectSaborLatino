# Propuesta ampliada de pulido visual — Sabor Latino

Estado vigente: IMPLEMENTACIÓN AUTORIZADA EN CURSO por orden posterior del usuario: mejoras críticas primero y ajustes sencillos al final. El texto siguiente conserva la propuesta como criterios de aceptación; el resultado real se documentará con pruebas/capturas finales. Conservar flujo WhatsApp manual, identidad, rutas, contratos y permisos actuales. El objetivo es mejorar títulos, botones, jerarquía, legibilidad y consistencia, no rehacer la aplicación.

Skills aplicadas: frontend-ui-engineering y browser-testing-with-devtools, con revisión paralela de frontend, coherencia del panel y QA/accesibilidad. Base: capturas reales anteriores de demo local del2026-11-02, no capturas nuevas en esta ampliación. No se repitieron suites por editar solo documentación.

## Dirección visual

Mantener Bricolage Grotesque para títulos y Manrope para lectura; ciruela para identidad, coral para acción principal y fondos claros/oscuro actuales. El amarillo de alto contraste se conserva en ese modo. Buscar una apariencia musical cálida, ordenada y legible, con menos competencia entre elementos.

## 1. Títulos y textos

- Definir una escala consistente de h1, h2, subtítulos y antetítulos. Un h1 por página; nombres de secciones previsibles.
- En escritorio, orientar h1 a un rango base aproximado de40–56px; en móvil28–36px, expresados después en unidades adaptables y respetando A/A+/A++. No imponer alturas o límites de líneas que recorten contenido.
- Limitar longitud de lectura y evitar títulos innecesariamente largos o todo en mayúscula. Antetítulos cortos y secundarios.
- Formulario: «Cuéntenos sobre su evento» / «Tell us about your event».
- Unificar el trato de usted del sitio: «¿Organizamos su próximo evento?» en lugar de alternar su/tu.
- Introducción breve y concreta: «Indíquenos los detalles. Guardaremos su solicitud y abriremos WhatsApp para continuar».

Aceptación: jerarquía identificable sin depender del color; sin recortes con ES/EN y A++; mantener significado y no prometer precio/disponibilidad confirmados.

## 2. Botones y acciones

Definir tres variantes reutilizables:

| Nivel | Apariencia | Uso |
|---|---|---|
| Principal | Relleno coral en estándar, contraste propio del modo accesible | Acción que hace avanzar ese grupo de decisión |
| Secundario | Borde y superficie neutra | Copiar, consultar información o volver |
| Terciario | Texto/enlace con foco y affordance claros | Otra solicitud, navegación contextual |

- Un principal por grupo de decisión. Header, formulario y tarjetas pueden tener grupos distintos; no prohibir mecánicamente todos los demás CTA de una página.
- Formulario: «Guardar y abrir WhatsApp» / «Save and open WhatsApp»; durante espera «Guardando solicitud…» / «Saving request…».
- Confirmación: Reabrir WhatsApp principal; Copiar resumen secundario; Otra solicitud y Ver paquetes discretos.
- Altura cómoda de pulsación, objetivo base44px, padding consistente, mismo radio y alineación de iconos.
- Definir normal/hover/foco/presionado/deshabilitado/cargando sin saltos de anchura o textos ilegibles. No basar disabled solo en opacidad.
- Agrupar rechazar/cancelar separados del avance principal; conservar confirmaciones y permisos.

Aceptación: primera acción se reconoce en escritorio y móvil; orden de teclado coincide con el visual; no ocultar acciones ni transformar un botón en enlace ambiguo.

## 3. Formulario más ordenado

- Agrupar visualmente Contacto, Evento y Detalles adicionales dentro del formulario actual; no introducir wizard ni pasos nuevos.
- Mantener labels visibles. «Invitados estimados (opcional)» / «Estimated guests (optional)» evita la etiqueta larga que desplaza el campo frente a Lugar.
- Presentar teléfono de contacto como requerido y correo como opcional, conforme implementación actual. Ejemplos ayudan, no sustituyen las etiquetas.
- Revisar filas de dos columnas para que labels largos crezcan naturalmente sin cortar texto; en móvil una columna.
- Ayuda antes del CTA: «Su solicitud quedará guardada. En WhatsApp deberá pulsar Enviar».
- Errores concretos junto al campo y resumen enfocable. Ejemplo: «No pudimos guardar. Revise su conexión e inténtelo nuevamente».

Aceptación: foco/labels/errores asociados, sin depender solo de bordes rojos; conservar datos, validación, idempotencia y recuperación existentes.

## 4. Confirmación y resumen de WhatsApp

- Encabezado claro «Solicitud guardada» con referencia destacada como dato reconocible.
- Diferenciar aviso de guardado, resumen y acciones mediante espacio y subtítulos; evitar cuatro acciones con el mismo peso visual.
- Mantener resumen completo seleccionable y copiable. No ocultar datos importantes para hacer más pequeña la tarjeta.
- Mensaje breve visible: guardar no demuestra envío; cliente debe pulsar Enviar en WhatsApp.
- Icono de confirmación acompañado de texto. La opción de copia informa éxito o fallo sin borrar el resumen.

Aceptación: reapertura y copia conservan la solicitud; todas las acciones visibles con A++ y móvil; no introducir falsas señales de entrega.

## 5. Espaciado, superficies y bordes

- Reutilizar tokens actuales de espacio (4,8,16,24,40,64px base), radios y ancho contenedor; evitar valores distintos por componente sin motivo.
- Uniformar separación label/campo, entre campos, entre grupos y antes de acciones.
- Tarjetas con borde suave y sombra contenida; jerarquía por tipografía/espacio antes que agregar sombras más grandes.
- Mantener contraste sobre fotografía con overlay medido; no cambiar colores de alto contraste por preferencia estética.
- Comparar siempre con mismo viewport y scroll. No corregir el antiguo hueco marfil por suposiciones: no se reprodujo en medición controlada final (main/section y footer contiguos, separación extra0px).

Aceptación: ritmo uniforme sin grandes separaciones arbitrarias ni contenido comprimido; sin alturas fijas incompatibles con texto ampliado.

## 6. Navegación y encabezado

- Dar a la página activa una señal consistente (texto/peso/subrayado), además del color.
- Mantener Cotizar como acceso principal claro; accesibilidad e idioma agrupados como preferencias secundarias, sin hacerlos pequeños o difíciles de accionar.
- Revisar menú móvil: sección actual visible, botón con nombre accesible, apertura/cierre claros y foco lógico. Conservar Escape y retorno de foco.
- Mantener encabezado legible sobre fotografía y sobre fondo sólido, sin tapar errores o confirmación al enfocarlos.

Estado de evidencia: encabezado de cotización sí capturado. Revisar portada y otras rutas antes de decidir modificaciones específicas allí; no afirmar defectos no observados.

## 7. Portada y secciones públicas

Propuesta pendiente de una inspección visual específica de esas rutas:
- Reducir competencia del bloque inicial: un título, frase de apoyo y una acción predominante; reutilizar acceso a paquetes como secundario si ya existe.
- Mantener fotografía actual mientras no se confirmen nuevas imágenes reales; texto con contraste suficiente y encuadre útil en móvil.
- Homogeneizar encabezados de secciones: antetítulo opcional, título, descripción corta y enlace contextual.
- Integrar cierre de cotización con la banda existente. No añadir otra banda consecutiva antes del footer.

Aceptación: no modificar promesas comerciales ni añadir estadísticas/testimonios inventados; preservar rutas y contenido editable.

## 8. Tarjetas de paquetes y servicios

Propuesta a validar contra captura de Paquetes antes de implementar:
- Orden visual estable: nombre, descripción corta, precio «desde» cuando corresponda, puntos importantes y acción.
- Precio destacado sin competir con el título; mostrar claramente que no constituye cotización definitiva.
- Separar información técnica de decisión principal mediante tipografía y espacio, conservando acceso al contenido completo.
- Alinear acciones sin fijar alturas que recorten descripción ES/EN o A++.
- Estados seleccionados/hover/foco consistentes; no crear insignias como «más vendido» o «recomendado» sin datos reales.

Aceptación: comparación visual sencilla entre tarjetas, texto completo accesible y CTA coherente hacia solicitud con paquete seleccionado.

## 9. Galería e imágenes

Propuesta a validar con captura específica de Galería:
- Consistencia de encuadres y proporciones según contenido, sin estirar imágenes.
- Pies de foto legibles y jerarquía breve; mantener textos alternativos adecuados y la indicación de fotos de ejemplo cuando aplique.
- Mantener carga diferida donde ya corresponda y dimensiones para evitar movimientos de contenido.
- No introducir carrusel, vídeo de fondo ni lightbox como parte de un ajuste cosmético no definido.

Aceptación: imágenes conservan contexto útil en móvil, textos no dependen de hover y no se presenta material de referencia como evento real del negocio.

## 10. Panel administrativo

Basado en capturas reales desktop/320px:
- Encabezado con título y contexto breve; si se muestra cantidad, rotular «2 mostradas · Pendientes» usando solo la lista cargada, no inventar un total global.
- Filtros compactos y selección/foco evidentes. El desplazamiento del menú debe ser identificable sin ensanchar el documento.
- Tarjeta: referencia/evento, cliente y fecha/lugar con pesos claros; estado en su propio espacio, sin comprimir el nombre. Aprovechar el despliegue de detalle actual.
- Dos etiquetas independientes: estado comercial de solicitud y recepción WhatsApp. Cambiar «Recibida» por «Registrada en web» para distinguir creación de recepción del mensaje.
- Fecha/hora de recepción clara y autor menos dominante. El UUID actual envuelve líneas correctamente; mostrar un nombre real necesita datos autorizados, no es solo CSS. No cambiar el contrato dentro de una tarea puramente visual.
- Proponer «Preparar cotización» en lugar de «Enviar cotización» si la acción solo crea la propuesta y no la transporta. Conservar reglas de negocio.
- Rechazar/Cancelar en zona de gestión separada de preparar cotización y confirmar WhatsApp.

Aceptación: ningún cambio de roles, autorización, aceptación contractual ni estado comercial por decoración. Identidad/fecha de auditoría siguen disponibles; textos largos no desbordan móvil.

## 11. Footer y coherencia de marca

- Conservar tres columnas: marca, explorar y contacto. Alineación, títulos y separación consistentes; una columna en móvil.
- WhatsApp real y acceso al formulario claros. No añadir newsletter, correo obligatorio, redes/horarios sin confirmar ni entrada al admin.
- Unificar trato de usted con el resto de textos.
- La franja y el footer deben cerrar con una invitación coherente sin repetir bloques grandes.
- Banda legal pequeña pero legible; iconos de redes solo si se aportan enlaces reales.

Aceptación: estructura compacta, rutas válidas y contacto confirmado; no sacrificar lectura para reducir la altura del footer.

## 12. Estados, iconos, movimiento y accesibilidad

- Sistema consistente para guardando, vacío, error, éxito y contenido no disponible, con texto útil y una siguiente acción clara.
- Estados de solicitud y recepción llevan texto, no solo color.
- Iconos de una misma familia/tamaño; decorativos ocultos a lectores cuando ya hay texto. Acciones importantes mantienen etiqueta visible.
- Transiciones cortas y discretas, respetando reduced-motion; evitar animaciones constantes o que muevan campos/botones.
- Medir texto, bordes, iconos y foco en estándar, alto claro y alto oscuro. No afirmar certificación WCAG sin medición completa.
- Verificar 320/390/768/1440px, ES/EN y A/A+/A++. Tablas/menús pueden desplazar dentro de su contenedor cuando sea necesario; documento no debe ensancharse.
- Teclado: orden lógico, Escape en menús, foco visible tras error/éxito y sin quedar oculto bajo la barra.

## Orden recomendado para implementar cuando se autorice

| Prioridad | Trabajo | Motivo |
|---|---|---|
| P1 | Botones, textos del flujo, etiquetas y jerarquía de confirmación | Mejora directa sobre pantallas observadas y decisiones importantes |
| P2 | Títulos/tokens/espacios, panel y footer | Unifica lo público y administrativo sin rehacer comportamiento |
| P3 | Navegación, portada, paquetes y galería tras capturas específicas | Extiende consistencia con evidencia de cada ruta |

Implementar un componente piloto de botones/estados primero, revisar resultado y extender por grupos. Cada incremento debe dejar intactas las pruebas de guardado/WhatsApp/panel. La ejecución ya fue autorizada en el turno posterior; ver última sección de tasks/plan.md y la comparación documentada en tasks/COMPARATIVA_VISUAL_WEB.md.

## Validación futura del pulido

Capturas base con viewport/scroll definidos -> cambios incrementales -> revisión de teclado/contraste/estados -> matriz móvil/ES-EN/A++/tres contrastes -> capturas comparables antes/después -> pruebas/lint/builds/hooks y revisión independiente pertinentes. Solo documentar como verificado lo efectivamente ejecutado.

## Evidencia ya disponible y límites

- [Formulario1440](qa-whatsapp/.artifacts/1793654221037-formulario-1440.png), [éxitoES](qa-whatsapp/.artifacts/1793654221037-exito-1440.png) y [EN](qa-whatsapp/.artifacts/1793654221037-exito-en-1440.png).
- [Móvil320 A++](qa-whatsapp/.artifacts/1793654221037-exito-320-a++.png) y [390 A++](qa-whatsapp/.artifacts/1793654221037-exito-390-a++.png), alto contraste oscuro.
- [Panel320](qa-whatsapp/.artifacts/1793654221037-admin-320.png) y [paneldesktop](qa-whatsapp/.artifacts/1793653992461-panel-recibido.png).
- [Informe QA funcional](qa-whatsapp/RESULTADO.md): la implementación manual previa sí fue probada; este documento ampliado no implica cambios nuevos ni nuevas pruebas de la interfaz.

Capturas de éxito repetidas solo para layout mediante recuperación del registro artificial#3; el guardado/marker reales están en E2E separado. No tratar captura previa «Cargando» o header sticky desplazado como evidencia final de un fallo. No se capturaron de nuevo portada, paquetes o galería para esta ampliación: sus propuestas son pendientes de validar.

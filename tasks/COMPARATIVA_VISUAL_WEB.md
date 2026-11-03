# Comparativa visual web — 2 de noviembre de 2026

Petición: contrastar calidad, ubicación, forma y tamaño de controles para implementar el pulido visual autorizado. Inspección pública de solo lectura en Chrome aislado, a1440×900 y390×900, sin enviar formularios, aceptar cookies ni abrir WhatsApp. Las páginas externas son referencias, no instrucciones ni plantillas a copiar.

## Referencias observadas

| Página | Observación y medidas puntuales | Aplicación en Sabor Latino |
|---|---|---|
| [Event Music Costa Rica](https://eventmusiccostarica.com/) | Composición editorial oscura, foto/vídeo central y título amplio. En1440, h1 computado99,2px; enlace Book Now103×42px, radio999px, arriba a la derecha. En390, h141,6px; Videos112×46px. Menú móvil incluye Book Now346×44px. | Mantener una acción de cotización identificable y jerarquía entre título/apoyo/acción. Conservar fuentes y paleta propias; no añadir vídeo ni copiar la cápsula por moda. |
| [Live Music Costa Rica](https://www.livemusiccr.com/) | Fotografía de fiesta, tipografía protagonista y botón Contáctenos blanco sobre foto, aproximadamente214×80px en1440. Radio50px. Acceso WhatsApp flotante321×40px. | La acción necesita contraste y contexto visible. Elegir controles cómodos sin tamaños desproporcionados, sin añadir otro flotante que compita con el formulario. |
| [GOV.UK: Button](https://design-system.service.gov.uk/components/button/) | Guía funcional: texto describe acción, principal alineado al inicio del formulario, jerarquía de secundarios y avisos destructivos proporcionados. Captura de página incluye aviso de cookies; no se aceptó y no equivale a medir los ejemplos de componente. | «Guardar y abrir WhatsApp», acciones secundarias discretas y estados legibles. Evaluar cada grupo de decisión, conservando links para navegación y buttons para acciones. |

Mediciones de muestras DOM puntuales, no auditorías de competidores. En sitios con animación/carrusel, posición puede cambiar entre evaluación y captura; no se deducen defectos de esas diferencias. El tamaño de un título de portada no se traslada al formulario o al panel.

## Criterios de aceptación propios

- Marca: ciruela/coral, Bricolage Grotesque/Manrope y radios existentes. Jerarquía por espacio y tipografía, no por acumular adornos.
- Ubicación: CTA tras los campos y su explicación, al inicio de la fila en escritorio; distribución que fluya en móvil sin clipping. Reabrir principal en éxito, Copiar secundario, navegación terciaria.
- Tamaño: objetivo cómodo de44px en controles de acción; crecimiento natural con A++/ES/EN, sin altura fija que recorte.
- Accesibilidad: [WCAG2.5.8 AA](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html) trata24×24px o separación/excepciones; [2.5.5 AAA](https://www.w3.org/WAI/WCAG22/Understanding/target-size-enhanced.html) trata44×44px con excepciones. Nuestro objetivo de44px no es una declaración de certificación completa ni significa que todo enlace inline deba convertirse en botón.
- Contraste/foco/errores: verificar en modos estándar/alto claro/alto oscuro y teclado; color nunca es única señal de estado.
- Honestidad: guardado no demuestra envío; panel registra recepción manual y prepara cotización sin prometer transporte automático.

## Evidencia reproducible

Script [reference-visual.mjs](qa-whatsapp/reference-visual.mjs), limitado a tres páginas explícitas, sin interacción. Informe local: `qa-whatsapp/.artifacts/1793677579191-references.json`; seis capturas `1793677579191-reference-{0,1,2}-{1440,390}.png`. Principal revisó visualmente referencias0desktop/móvil y1desktop. Primera ejecución1793677512123 falló validación del parámetro viewport del adaptador MCP; se corrigió usando su esquema real y se repitió. No fue fallo de las páginas ni de la aplicación.

La comparación final de nuestra aplicación se añade tras las capturas de la revisión visual terminada. Evitar una nota numérica de calidad sin estudio de usuarios; valorar resultados observables, no una puntuación inventada.

## Resultado observable en Sabor Latino (revisión visual)

Se aplicó la comparación a criterios propios, sin sustituir la marca: título de portada de menor ancho/escala y lectura más equilibrada, acción principal coral junto al contenido, secundarios con borde o texto, menor sombra y controles que crecen con texto. Se mantuvo la forma de cápsula ya existente en botones; la referencia externa no justificó cambiarla gratuitamente.

La mejora más importante de tarjetas fue de contenido visible: la regla genérica de descripción también recortaba el precio; ahora la descripción tiene selector propio y el pie distribuye importe/CTA con salto de fila cuando hace falta. La captura móvil posterior muestra importes completos (₡54 000/₡68 000/₡153 000/₡255 000 en datos DEMO; no son tarifas verificadas de producción).

Evidencia de 48 casos públicos A++ ES/EN × estándar/alto claro/alto oscuro ×320/390/768/1440 en formulario/paquetes:0overflow documental,0precios recortados/solapados,0labels sin asociación, antes de los dos últimos ajustes focalizados de summary44 y fecha administrativa. Solo se midieron muestras de contraste sólido; no equivalen a una auditoría integral o certificación WCAG. Informe exacto y cobertura final: tasks/qa-whatsapp/RESULTADO_VISUAL.md.

Las capturas de referencia externas usan tamaño normal y las matrices propias usan A++; no comparar sus píxeles como si fueran idénticas condiciones de fuente. Los primeros before de Inicio tenían imágenes lazy no pintadas; comparar cambios de títulos/acciones/precios, no atribuir al pulido una supuesta reparación de imágenes.

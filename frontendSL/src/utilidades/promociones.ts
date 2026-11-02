import type { Paquete, PromocionTemporada } from '../api/tipos';
import type { PromocionAplicablePaquete } from '../componentes/TarjetaPaquete';

export function obtenerPromocionParaPaquete(
  paquete: Paquete,
  promociones: PromocionTemporada[] | null | undefined,
  idioma: 'es' | 'en',
): PromocionAplicablePaquete | undefined {
  if (!Array.isArray(promociones)) return undefined;
  for (const promo of promociones) {
    if (!promo.activo || promo.vigente === false) continue;
    const item = promo.paquetes?.find((pk) => pk.paqueteId === paquete.id);
    if (item) {
      const etiqueta =
        idioma === 'en'
          ? promo.etiquetaTemporadaEn || promo.nombreEn || promo.etiquetaTemporada || promo.nombre
          : promo.etiquetaTemporada || promo.nombre;
      return {
        id: promo.id,
        etiqueta,
        porcentajeDescuento: promo.porcentajeDescuento,
        precioPromocional: item.precioPromocional,
      };
    }
  }
  return undefined;
}

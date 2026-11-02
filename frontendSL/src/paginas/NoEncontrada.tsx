import { Link } from 'react-router-dom';
import { usePreferencias } from '../preferencias/PreferenciasContext';

export function NoEncontrada() {
  const { t } = usePreferencias();

  return (
    <section className="seccion contenedor contenedor--angosto centrado">
      <p className="no-encontrada__codigo" aria-hidden="true">
        404
      </p>
      <div className="encabezado encabezado--centro">
        <h1 className="titulo-seccion">{t('Esta página no existe', 'Page not found')}</h1>
        <p className="intro">
          {t(
            'Es posible que la dirección esté mal escrita o que el contenido se haya trasladado.',
            'The address may be mistyped or the page may have moved.',
          )}
        </p>
      </div>
      <div className="acciones acciones--centradas">
        <Link to="/" className="boton boton--accion">
          {t('Volver al inicio', 'Back to home')}
        </Link>
        <Link to="/paquetes" className="boton boton--borde">
          {t('Ver paquetes', 'View packages')}
        </Link>
      </div>
    </section>
  );
}

import { Link } from 'react-router-dom';
import type { ImagenGaleria, Paquete, Servicio } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { IconoCheck, IconoFlecha } from '../componentes/Iconos';
import { TarjetaPaquete } from '../componentes/TarjetaPaquete';
import { useDatos } from '../hooks/useDatos';
import { formatoColones } from '../utilidades/formato';
import { FOTO_COTIZAR, FOTO_PORTADA, GALERIA_EJEMPLO } from '../utilidades/fotos';

const PASOS = [
  { titulo: 'Cuéntenos su evento', texto: 'Fecha, lugar, cantidad de invitados y el paquete que le interesa.' },
  { titulo: 'Reciba su cotización', texto: 'Le enviamos un precio claro en colones con el equipo y las horas incluidas.' },
  { titulo: 'Disfrute la fiesta', texto: 'Llegamos antes, montamos todo y la música suena desde el primer minuto.' },
];

export function Inicio() {
  const servicios = useDatos<Servicio[]>('/api/servicios');
  const paquetes = useDatos<Paquete[]>('/api/paquetes');
  const galeria = useDatos<ImagenGaleria[]>('/api/galeria');

  // Datos reales para la portada (si todavía no cargan, no se muestran)
  const precioMinimo = paquetes.datos?.length ? Math.min(...paquetes.datos.map((p) => p.precioBase)) : null;
  const fotosGaleria = galeria.datos?.length ? galeria.datos.slice(0, 3) : null;

  return (
    <>
      <section className="portada" aria-labelledby="titulo-portada">
        <img className="portada__foto" src={FOTO_PORTADA} alt="" width={2000} height={1333} fetchPriority="high" />
        <div className="contenedor portada__interior">
          <span className="portada__etiqueta">Bodas · Quince años · Cumpleaños · Empresas</span>
          <h1 id="titulo-portada" className="portada__titulo">
            Que su fiesta <em>suene en vivo</em>
          </h1>
          <p className="portada__texto">
            Karaoke, marimba, orquesta y sonido profesional en todo el Valle Central. Usted elige el paquete; nosotros
            llevamos la música, el equipo y la gente que lo hace sonar.
          </p>
          <div className="acciones">
            <Link to="/solicitar" className="boton boton--accion">
              Solicitar cotización
              <IconoFlecha />
            </Link>
            <Link to="/paquetes" className="boton boton--vidrio">
              Ver paquetes y precios
            </Link>
          </div>
          {paquetes.datos && precioMinimo !== null && (
            <div className="datos">
              <div className="dato">
                <strong>{paquetes.datos.length}</strong>
                <span>paquetes listos</span>
              </div>
              <div className="dato">
                <strong>{formatoColones(precioMinimo)}</strong>
                <span>precio desde</span>
              </div>
              <div className="dato">
                <strong>Equipo</strong>
                <span>incluido en cada paquete</span>
              </div>
            </div>
          )}
        </div>
        <span className="credito">Foto de ejemplo</span>
      </section>

      <section className="seccion contenedor" aria-labelledby="titulo-pasos">
        <div className="encabezado">
          <p className="antetitulo">Cómo funciona</p>
          <h2 id="titulo-pasos" className="titulo-seccion">
            Tres pasos y a bailar
          </h2>
        </div>
        <ol className="pasos">
          {PASOS.map((paso, i) => (
            <li key={paso.titulo} className="paso">
              <span className="paso__n" aria-hidden="true">
                {i + 1}
              </span>
              <h3>{paso.titulo}</h3>
              <p>{paso.texto}</p>
            </li>
          ))}
        </ol>
      </section>

      <section className="seccion seccion--suave" aria-labelledby="titulo-paquetes">
        <div className="contenedor">
          <div className="encabezado encabezado--fila">
            <div className="encabezado encabezado--sin-margen">
              <p className="antetitulo">Paquetes</p>
              <h2 id="titulo-paquetes" className="titulo-seccion">
                Elija cómo quiere que suene
              </h2>
            </div>
            <Link to="/paquetes" className="enlace">
              Ver todos los paquetes →
            </Link>
          </div>

          {servicios.datos && servicios.datos.length > 0 && (
            <div className="servicios servicios--separados">
              {servicios.datos.map((s) => (
                <Link key={s.id} to={`/paquetes?servicio=${s.id}`} className="servicio">
                  <h3>{s.nombre}</h3>
                  {s.descripcion && <p>{s.descripcion}</p>}
                </Link>
              ))}
            </div>
          )}

          {paquetes.cargando && <Cargando />}
          {paquetes.error && <Mensaje tipo="error">{paquetes.error}</Mensaje>}
          {paquetes.datos && (
            <div className="paquetes paquetes--tres">
              {paquetes.datos.slice(0, 3).map((p) => (
                <TarjetaPaquete key={p.id} paquete={p} />
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="seccion contenedor" aria-labelledby="titulo-galeria">
        <div className="encabezado encabezado--fila">
          <div className="encabezado encabezado--sin-margen">
            <p className="antetitulo">Galería</p>
            <h2 id="titulo-galeria" className="titulo-seccion">
              Así se viven nuestras noches
            </h2>
          </div>
          <Link to="/galeria" className="enlace">
            Ver la galería →
          </Link>
        </div>
        {!fotosGaleria && <span className="nota-ejemplo">Fotos de ejemplo</span>}
        <div className="galeria">
          {(fotosGaleria ?? GALERIA_EJEMPLO.filter((f) => f.alto === 467).slice(0, 3)).map((img) => (
            <figure key={img.id} className="galeria__item">
              <img src={img.imagenUrl} alt={img.titulo} width={700} height={467} loading="lazy" decoding="async" />
              <figcaption>{img.titulo}</figcaption>
            </figure>
          ))}
        </div>
      </section>

      <section className="seccion banda" aria-labelledby="titulo-cotizar">
        <img className="banda__foto" src={FOTO_COTIZAR} alt="" width={1800} height={1200} loading="lazy" />
        <div className="contenedor banda__grid">
          <div className="encabezado encabezado--sin-margen">
            <p className="antetitulo">Solicitar cotización</p>
            <h2 id="titulo-cotizar" className="titulo-seccion">
              Aparte su fecha antes de que se la ganen
            </h2>
            <p className="intro">Pedir la cotización no tiene costo ni compromiso.</p>
            <ul className="ventajas">
              <li>
                <IconoCheck />
                Precio claro en colones
              </li>
              <li>
                <IconoCheck />
                Equipo y montaje incluidos
              </li>
              <li>
                <IconoCheck />
                Si tiene cuenta, la sigue desde “Mi cuenta”
              </li>
            </ul>
          </div>
          <div className="acciones acciones--centro">
            <Link to="/solicitar" className="boton boton--accion">
              Solicitar cotización
              <IconoFlecha />
            </Link>
            <Link to="/preguntas" className="boton boton--vidrio">
              Ver preguntas frecuentes
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}

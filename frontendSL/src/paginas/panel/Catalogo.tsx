import { GaleriaAdmin } from './GaleriaAdmin';
import { useState } from 'react';
import type { Equipo, Paquete, PreguntaFrecuente, Servicio } from '../../api/tipos';
import { Crud } from '../../componentes/panel/Crud';
import { useDatos } from '../../hooks/useDatos';
import { formatoColones } from '../../utilidades/formato';
import { PersonalAdmin } from './PersonalAdmin';
import { PromocionesAdmin } from './PromocionesAdmin';

const SECCIONES = [
  { clave: 'servicios', texto: 'Servicios' },
  { clave: 'paquetes', texto: 'Paquetes' },
  { clave: 'promociones', texto: 'Promociones' },
  { clave: 'equipo', texto: 'Equipo' },
  { clave: 'personal', texto: 'Personal' },
  { clave: 'galeria', texto: 'Galería' },
  { clave: 'preguntas', texto: 'Preguntas' },
] as const;

type Seccion = (typeof SECCIONES)[number]['clave'];

const texto = (v: unknown) => (v === null || v === undefined ? '' : String(v).trim());
const textoONulo = (v: unknown) => texto(v) || null;
const numero = (v: unknown) => Number(v) || 0;

// Catálogo del negocio: lo que ve el público (servicios, paquetes técnicos, promociones, galería, preguntas)
// y lo que se usa en los eventos (equipo y personal)
export function Catalogo() {
  const [seccion, setSeccion] = useState<Seccion>('servicios');

  return (
    <>
      <div className="filtros" role="group" aria-label="Sección del catálogo">
        {SECCIONES.map((s) => (
          <button
            key={s.clave}
            type="button"
            className="chip"
            aria-pressed={seccion === s.clave}
            onClick={() => setSeccion(s.clave)}
          >
            {s.texto}
          </button>
        ))}
      </div>

      {seccion === 'servicios' && <Servicios />}
      {seccion === 'paquetes' && <Paquetes />}
      {seccion === 'promociones' && <PromocionesAdmin />}
      {seccion === 'equipo' && <Equipos />}
      {seccion === 'personal' && <PersonalAdmin />}
      {seccion === 'galeria' && <GaleriaAdmin />}
      {seccion === 'preguntas' && <Preguntas />}
    </>
  );
}

function Servicios() {
  return (
    <Crud<Servicio>
      titulo="Servicios"
      ruta="/api/servicios"
      columnas={[
        {
          etiqueta: 'Nombre',
          valor: (s) => (
            <span>
              <strong>{s.nombre}</strong>
              {s.nombreEn && <span className="texto-suave"> · EN: {s.nombreEn}</span>}
            </span>
          ),
        },
        { etiqueta: 'Descripción', valor: (s) => s.descripcion ?? '—' },
      ]}
      campos={[
        { nombre: 'nombre', etiqueta: 'Nombre (Español)', requerido: true, max: 100 },
        { nombre: 'nombreEn', etiqueta: 'Nombre (Inglés)', max: 100 },
        { nombre: 'descripcion', etiqueta: 'Descripción (Español)', tipo: 'area', max: 1000 },
        { nombre: 'descripcionEn', etiqueta: 'Descripción (Inglés)', tipo: 'area', max: 1000 },
        { nombre: 'imagenUrl', etiqueta: 'Fotografía', tipo: 'imagen', max: 500 },
        { nombre: 'activo', etiqueta: 'Activo (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ nombre: '', nombreEn: '', descripcion: '', descripcionEn: '', imagenUrl: '', activo: true }}
      aApi={(v) => ({
        nombre: texto(v.nombre),
        nombreEn: textoONulo(v.nombreEn),
        descripcion: textoONulo(v.descripcion),
        descripcionEn: textoONulo(v.descripcionEn),
        imagenUrl: textoONulo(v.imagenUrl),
        activo: Boolean(v.activo),
      })}
    />
  );
}

type FilaEquipo = { equipoId: number; cantidad: number };

function Paquetes() {
  const servicios = useDatos<Servicio[]>('/api/servicios?incluirInactivos=true');
  const equipos = useDatos<Equipo[]>('/api/equipos');

  return (
    <Crud<Paquete>
      titulo="Paquetes"
      ruta="/api/paquetes"
      columnas={[
        {
          etiqueta: 'Nombre',
          valor: (p) => (
            <span>
              <strong>{p.nombre}</strong>
              {p.nombreEn && <div className="texto-suave">EN: {p.nombreEn}</div>}
            </span>
          ),
        },
        { etiqueta: 'Servicio', valor: (p) => p.servicioNombre },
        { etiqueta: 'Precio desde', valor: (p) => <span className="numero">{formatoColones(p.precioBase)}</span> },
        {
          etiqueta: 'Ficha técnica',
          valor: (p) =>
            [p.potenciaSonido, p.coberturaAforo, p.duracionMontaje].filter(Boolean).join(' · ') || '—',
        },
        { etiqueta: 'Equipo', valor: (p) => p.equipos.map((e) => `${e.cantidad} × ${e.equipoNombre}`).join(', ') || '—' },
      ]}
      campos={[
        {
          nombre: 'servicioId',
          etiqueta: 'Servicio',
          tipo: 'select',
          requerido: true,
          opciones: servicios.datos?.map((s) => ({ valor: s.id, texto: s.nombre })) ?? [],
        },
        { nombre: 'nombre', etiqueta: 'Nombre (Español)', requerido: true, max: 100 },
        { nombre: 'nombreEn', etiqueta: 'Nombre (Inglés)', max: 100 },
        { nombre: 'descripcion', etiqueta: 'Descripción (Español)', tipo: 'area', max: 1000 },
        { nombre: 'descripcionEn', etiqueta: 'Descripción (Inglés)', tipo: 'area', max: 1000 },
        { nombre: 'precioBase', etiqueta: 'Precio desde (₡)', tipo: 'numero', requerido: true, min: 0, paso: '1000' },
        {
          nombre: 'potenciaSonido',
          etiqueta: 'Potencia acústica',
          max: 100,
          ayuda: 'Ej.: 4 000 W RMS con subwoofers activos de 18".',
        },
        {
          nombre: 'coberturaAforo',
          etiqueta: 'Cobertura de aforo sugerida',
          max: 100,
          ayuda: 'Ej.: Hasta 180 invitados en salón o jardín.',
        },
        {
          nombre: 'duracionMontaje',
          etiqueta: 'Tiempo de montaje y prueba acústica',
          max: 150,
          ayuda: 'Ej.: 90 minutos antes del inicio.',
        },
        {
          nombre: 'requerimientosElectricos',
          etiqueta: 'Requerimientos eléctricos',
          max: 250,
          ayuda: 'Ej.: 2 circuitos independientes de 120 V / 20 A.',
        },
        {
          nombre: 'espacioMinimo',
          etiqueta: 'Espacio mínimo en escenario',
          max: 150,
          ayuda: 'Ej.: 4 m × 2,5 m techado y nivelado.',
        },
        { nombre: 'fichaTecnica', etiqueta: 'Notas técnicas adicionales (Español)', tipo: 'area', max: 1500 },
        { nombre: 'fichaTecnicaEn', etiqueta: 'Notas técnicas adicionales (Inglés)', tipo: 'area', max: 1500 },
        { nombre: 'imagenUrl', etiqueta: 'Fotografía', tipo: 'imagen', max: 500 },
        { nombre: 'activo', etiqueta: 'Activo (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{
        servicioId: '',
        nombre: '',
        nombreEn: '',
        descripcion: '',
        descripcionEn: '',
        precioBase: '',
        potenciaSonido: '',
        coberturaAforo: '',
        duracionMontaje: '',
        requerimientosElectricos: '',
        espacioMinimo: '',
        fichaTecnica: '',
        fichaTecnicaEn: '',
        imagenUrl: '',
        activo: true,
        equipos: [],
      }}
      aFormulario={(p) => ({
        ...p,
        equipos: p.equipos.map((e) => ({ equipoId: e.equipoId, cantidad: e.cantidad })),
      })}
      aApi={(v) => ({
        servicioId: numero(v.servicioId),
        nombre: texto(v.nombre),
        nombreEn: textoONulo(v.nombreEn),
        descripcion: textoONulo(v.descripcion),
        descripcionEn: textoONulo(v.descripcionEn),
        precioBase: numero(v.precioBase),
        potenciaSonido: textoONulo(v.potenciaSonido),
        coberturaAforo: textoONulo(v.coberturaAforo),
        duracionMontaje: textoONulo(v.duracionMontaje),
        requerimientosElectricos: textoONulo(v.requerimientosElectricos),
        espacioMinimo: textoONulo(v.espacioMinimo),
        fichaTecnica: textoONulo(v.fichaTecnica),
        fichaTecnicaEn: textoONulo(v.fichaTecnicaEn),
        imagenUrl: textoONulo(v.imagenUrl),
        activo: Boolean(v.activo),
        equipos: (v.equipos as FilaEquipo[]).filter((e) => e.equipoId > 0 && e.cantidad > 0),
      })}
      extra={(v, cambiar) => {
        const filas = (v.equipos as FilaEquipo[]) ?? [];
        const poner = (nuevas: FilaEquipo[]) => cambiar('equipos', nuevas);
        return (
          <fieldset className="grupo-campos">
            <legend>Equipo incluido</legend>
            {filas.map((fila, i) => (
              <div className="fila-equipo" key={i}>
                <select
                  aria-label="Equipo"
                  name={`equipo-${i}`}
                  value={fila.equipoId || ''}
                  onChange={(e) => poner(filas.map((f, j) => (j === i ? { ...f, equipoId: Number(e.target.value) } : f)))}
                >
                  <option value="">Elija el equipo…</option>
                  {equipos.datos?.map((eq) => (
                    <option key={eq.id} value={eq.id}>
                      {eq.nombre}
                    </option>
                  ))}
                </select>
                <input
                  aria-label="Cantidad"
                  name={`cantidad-${i}`}
                  type="number"
                  min={1}
                  value={fila.cantidad}
                  onChange={(e) => poner(filas.map((f, j) => (j === i ? { ...f, cantidad: Number(e.target.value) } : f)))}
                />
                <button
                  type="button"
                  className="boton boton--texto boton--mini"
                  onClick={() => poner(filas.filter((_, j) => j !== i))}
                >
                  Quitar
                </button>
              </div>
            ))}
            <button
              type="button"
              className="boton boton--borde boton--mini"
              onClick={() => poner([...filas, { equipoId: 0, cantidad: 1 }])}
            >
              + Agregar equipo
            </button>
          </fieldset>
        );
      }}
    />
  );
}

function Equipos() {
  return (
    <Crud<Equipo>
      titulo="Equipo"
      ruta="/api/equipos"
      columnas={[
        {
          etiqueta: 'Nombre',
          valor: (e) => (
            <span>
              <strong>{e.nombre}</strong>
              {e.especificacionTecnica && <div className="texto-suave">{e.especificacionTecnica}</div>}
            </span>
          ),
        },
        { etiqueta: 'Tipo', valor: (e) => e.tipo ?? '—' },
        { etiqueta: 'Cantidad total', valor: (e) => <span className="numero">{e.cantidadTotal}</span> },
      ]}
      campos={[
        { nombre: 'nombre', etiqueta: 'Nombre (Español)', requerido: true, max: 100 },
        { nombre: 'nombreEn', etiqueta: 'Nombre (Inglés)', max: 100 },
        { nombre: 'tipo', etiqueta: 'Tipo', max: 50, ayuda: 'Por ejemplo Audio, Video o Iluminación.' },
        {
          nombre: 'especificacionTecnica',
          etiqueta: 'Especificación técnica (Español)',
          max: 250,
          ayuda: 'Modelo, potencia en vatios, respuesta de frecuencia o protocolo DMX.',
        },
        { nombre: 'especificacionTecnicaEn', etiqueta: 'Especificación técnica (Inglés)', max: 250 },
        { nombre: 'cantidadTotal', etiqueta: 'Cantidad total', tipo: 'numero', requerido: true, min: 0 },
        { nombre: 'activo', etiqueta: 'Activo', tipo: 'check' },
      ]}
      nuevo={{
        nombre: '',
        nombreEn: '',
        tipo: '',
        especificacionTecnica: '',
        especificacionTecnicaEn: '',
        cantidadTotal: '',
        activo: true,
      }}
      aApi={(v) => ({
        nombre: texto(v.nombre),
        nombreEn: textoONulo(v.nombreEn),
        tipo: textoONulo(v.tipo),
        especificacionTecnica: textoONulo(v.especificacionTecnica),
        especificacionTecnicaEn: textoONulo(v.especificacionTecnicaEn),
        cantidadTotal: numero(v.cantidadTotal),
        activo: Boolean(v.activo),
      })}
    />
  );
}




function Preguntas() {
  return (
    <Crud<PreguntaFrecuente>
      titulo="Preguntas frecuentes"
      ruta="/api/preguntas-frecuentes"
      columnas={[
        {
          etiqueta: 'Pregunta',
          valor: (p) => (
            <span>
              <strong>{p.pregunta}</strong>
              {p.preguntaEn && <div className="texto-suave">EN: {p.preguntaEn}</div>}
            </span>
          ),
        },
        { etiqueta: 'Respuesta', valor: (p) => p.respuesta },
        { etiqueta: 'Orden', valor: (p) => <span className="numero">{p.orden}</span> },
      ]}
      campos={[
        { nombre: 'pregunta', etiqueta: 'Pregunta (Español)', requerido: true, max: 300 },
        { nombre: 'preguntaEn', etiqueta: 'Pregunta (Inglés)', max: 300 },
        { nombre: 'respuesta', etiqueta: 'Respuesta (Español)', tipo: 'area', requerido: true, max: 2000 },
        { nombre: 'respuestaEn', etiqueta: 'Respuesta (Inglés)', tipo: 'area', max: 2000 },
        { nombre: 'orden', etiqueta: 'Orden', tipo: 'numero', min: 0, max: 1000 },
        { nombre: 'activo', etiqueta: 'Activa (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ pregunta: '', preguntaEn: '', respuesta: '', respuestaEn: '', orden: 0, activo: true }}
      aApi={(v) => ({
        pregunta: texto(v.pregunta),
        preguntaEn: textoONulo(v.preguntaEn),
        respuesta: texto(v.respuesta),
        respuestaEn: textoONulo(v.respuestaEn),
        orden: numero(v.orden),
        activo: Boolean(v.activo),
      })}
    />
  );
}

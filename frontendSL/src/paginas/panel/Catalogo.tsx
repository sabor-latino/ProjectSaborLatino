import { useState } from 'react';
import type { Empleado, Equipo, ImagenGaleria, Paquete, PreguntaFrecuente, Servicio, Usuario } from '../../api/tipos';
import { Crud } from '../../componentes/panel/Crud';
import { useDatos } from '../../hooks/useDatos';
import { formatoColones } from '../../utilidades/formato';

const SECCIONES = [
  { clave: 'servicios', texto: 'Servicios' },
  { clave: 'paquetes', texto: 'Paquetes' },
  { clave: 'equipo', texto: 'Equipo' },
  { clave: 'personal', texto: 'Personal' },
  { clave: 'galeria', texto: 'Galería' },
  { clave: 'preguntas', texto: 'Preguntas' },
] as const;

type Seccion = (typeof SECCIONES)[number]['clave'];

const texto = (v: unknown) => (v === null || v === undefined ? '' : String(v).trim());
const textoONulo = (v: unknown) => texto(v) || null;
const numero = (v: unknown) => Number(v) || 0;

// Catálogo del negocio: lo que ve el público (servicios, paquetes, galería, preguntas)
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
      {seccion === 'equipo' && <Equipos />}
      {seccion === 'personal' && <Personal />}
      {seccion === 'galeria' && <Galeria />}
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
        { etiqueta: 'Nombre', valor: (s) => <strong>{s.nombre}</strong> },
        { etiqueta: 'Descripción', valor: (s) => s.descripcion ?? '—' },
      ]}
      campos={[
        { nombre: 'nombre', etiqueta: 'Nombre', requerido: true, max: 100 },
        { nombre: 'descripcion', etiqueta: 'Descripción', tipo: 'area', max: 1000 },
        { nombre: 'imagenUrl', etiqueta: 'Dirección de la imagen', tipo: 'url', max: 500 },
        { nombre: 'activo', etiqueta: 'Activo (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ nombre: '', descripcion: '', imagenUrl: '', activo: true }}
      aApi={(v) => ({
        nombre: texto(v.nombre),
        descripcion: textoONulo(v.descripcion),
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
        { etiqueta: 'Nombre', valor: (p) => <strong>{p.nombre}</strong> },
        { etiqueta: 'Servicio', valor: (p) => p.servicioNombre },
        { etiqueta: 'Precio desde', valor: (p) => <span className="numero">{formatoColones(p.precioBase)}</span> },
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
        { nombre: 'nombre', etiqueta: 'Nombre', requerido: true, max: 100 },
        { nombre: 'descripcion', etiqueta: 'Descripción', tipo: 'area', max: 1000 },
        { nombre: 'precioBase', etiqueta: 'Precio desde (₡)', tipo: 'numero', requerido: true, min: 0, paso: '1000' },
        { nombre: 'imagenUrl', etiqueta: 'Dirección de la imagen', tipo: 'url', max: 500 },
        { nombre: 'activo', etiqueta: 'Activo (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ servicioId: '', nombre: '', descripcion: '', precioBase: '', imagenUrl: '', activo: true, equipos: [] }}
      aFormulario={(p) => ({
        ...p,
        equipos: p.equipos.map((e) => ({ equipoId: e.equipoId, cantidad: e.cantidad })),
      })}
      aApi={(v) => ({
        servicioId: numero(v.servicioId),
        nombre: texto(v.nombre),
        descripcion: textoONulo(v.descripcion),
        precioBase: numero(v.precioBase),
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
        { etiqueta: 'Nombre', valor: (e) => <strong>{e.nombre}</strong> },
        { etiqueta: 'Tipo', valor: (e) => e.tipo ?? '—' },
        { etiqueta: 'Cantidad total', valor: (e) => <span className="numero">{e.cantidadTotal}</span> },
      ]}
      campos={[
        { nombre: 'nombre', etiqueta: 'Nombre', requerido: true, max: 100 },
        { nombre: 'tipo', etiqueta: 'Tipo', max: 50, ayuda: 'Por ejemplo Audio, Video o Iluminación.' },
        { nombre: 'cantidadTotal', etiqueta: 'Cantidad total', tipo: 'numero', requerido: true, min: 0 },
        { nombre: 'activo', etiqueta: 'Activo', tipo: 'check' },
      ]}
      nuevo={{ nombre: '', tipo: '', cantidadTotal: '', activo: true }}
      aApi={(v) => ({
        nombre: texto(v.nombre),
        tipo: textoONulo(v.tipo),
        cantidadTotal: numero(v.cantidadTotal),
        activo: Boolean(v.activo),
      })}
    />
  );
}

function Personal() {
  const trabajadores = useDatos<Usuario[]>('/api/usuarios?rol=Trabajador');

  return (
    <Crud<Empleado>
      titulo="Personal"
      ruta="/api/empleados"
      columnas={[
        { etiqueta: 'Nombre', valor: (e) => <strong>{e.nombreCompleto}</strong> },
        { etiqueta: 'Cédula', valor: (e) => e.cedula },
        { etiqueta: 'Puesto', valor: (e) => e.puesto },
        { etiqueta: 'Teléfono', valor: (e) => e.telefono ?? '—' },
        {
          etiqueta: 'Cuenta',
          valor: (e) => trabajadores.datos?.find((u) => u.id === e.usuarioId)?.email ?? 'Sin cuenta',
        },
      ]}
      campos={[
        { nombre: 'nombreCompleto', etiqueta: 'Nombre completo', requerido: true, max: 150 },
        { nombre: 'cedula', etiqueta: 'Cédula', requerido: true, max: 20 },
        { nombre: 'puesto', etiqueta: 'Puesto', requerido: true, max: 100, ayuda: 'Por ejemplo Técnico de sonido o Cantante.' },
        { nombre: 'telefono', etiqueta: 'Teléfono', max: 30 },
        {
          nombre: 'usuarioId',
          etiqueta: 'Cuenta de trabajador (opcional)',
          tipo: 'select',
          ayuda: 'Con una cuenta, la persona ve sus eventos al iniciar sesión.',
          opciones: trabajadores.datos?.map((u) => ({ valor: u.id, texto: `${u.nombreCompleto} (${u.email})` })) ?? [],
        },
        { nombre: 'activo', etiqueta: 'Activo', tipo: 'check' },
      ]}
      nuevo={{ nombreCompleto: '', cedula: '', puesto: '', telefono: '', usuarioId: '', activo: true }}
      aApi={(v) => ({
        nombreCompleto: texto(v.nombreCompleto),
        cedula: texto(v.cedula),
        puesto: texto(v.puesto),
        telefono: textoONulo(v.telefono),
        usuarioId: textoONulo(v.usuarioId),
        activo: Boolean(v.activo),
      })}
    />
  );
}

function Galeria() {
  return (
    <Crud<ImagenGaleria>
      titulo="Galería"
      ruta="/api/galeria"
      columnas={[
        {
          etiqueta: 'Foto',
          valor: (g) => <img className="miniatura" src={g.imagenUrl} alt="" width={96} height={64} loading="lazy" />,
        },
        { etiqueta: 'Título', valor: (g) => <strong>{g.titulo}</strong> },
        { etiqueta: 'Orden', valor: (g) => <span className="numero">{g.orden}</span> },
      ]}
      campos={[
        { nombre: 'titulo', etiqueta: 'Título', requerido: true, max: 150 },
        { nombre: 'imagenUrl', etiqueta: 'Dirección de la imagen', tipo: 'url', requerido: true, max: 500 },
        { nombre: 'orden', etiqueta: 'Orden', tipo: 'numero', min: 0, max: 1000, ayuda: 'Las de menor número salen primero.' },
        { nombre: 'activo', etiqueta: 'Activa (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ titulo: '', imagenUrl: '', orden: 0, activo: true }}
      aApi={(v) => ({
        titulo: texto(v.titulo),
        imagenUrl: texto(v.imagenUrl),
        orden: numero(v.orden),
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
        { etiqueta: 'Pregunta', valor: (p) => <strong>{p.pregunta}</strong> },
        { etiqueta: 'Respuesta', valor: (p) => p.respuesta },
        { etiqueta: 'Orden', valor: (p) => <span className="numero">{p.orden}</span> },
      ]}
      campos={[
        { nombre: 'pregunta', etiqueta: 'Pregunta', requerido: true, max: 300 },
        { nombre: 'respuesta', etiqueta: 'Respuesta', tipo: 'area', requerido: true, max: 2000 },
        { nombre: 'orden', etiqueta: 'Orden', tipo: 'numero', min: 0, max: 1000 },
        { nombre: 'activo', etiqueta: 'Activa (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ pregunta: '', respuesta: '', orden: 0, activo: true }}
      aApi={(v) => ({
        pregunta: texto(v.pregunta),
        respuesta: texto(v.respuesta),
        orden: numero(v.orden),
        activo: Boolean(v.activo),
      })}
    />
  );
}

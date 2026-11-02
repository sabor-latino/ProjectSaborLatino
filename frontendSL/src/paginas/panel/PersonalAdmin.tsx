import type { Empleado, Usuario } from '../../api/tipos';
import { Crud } from '../../componentes/panel/Crud';
import { useDatos } from '../../hooks/useDatos';

const texto = (v: unknown) => (v === null || v === undefined ? '' : String(v).trim());
const textoONulo = (v: unknown) => texto(v) || null;

export function PersonalAdmin() {
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

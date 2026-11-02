import { useState } from 'react';
import { api } from '../../api/cliente';
import type { Perfil } from '../../api/tipos';
import { useAuth } from '../../auth/contexto';
import { Mensaje } from '../../componentes/Estado';
import { TituloPanel } from '../../componentes/panel/Piezas';
import { formatoFecha } from '../../utilidades/formato';

export function MiPerfilAdmin() {
  const { perfil, actualizarPerfilLocal } = useAuth();
  const [nombreCompleto, setNombreCompleto] = useState(perfil?.nombreCompleto ?? '');
  const [telefono, setTelefono] = useState(perfil?.telefono ?? '');
  const [guardando, setGuardando] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);

  if (!perfil) return null;

  const guardar = async (e: React.FormEvent) => {
    e.preventDefault();
    setGuardando(true);
    setAviso(null);
    try {
      const resp = await api.put<Perfil>('/api/cuenta/yo', {
        nombreCompleto: nombreCompleto.trim(),
        telefono: telefono.trim() || null,
      });
      actualizarPerfilLocal?.(resp);
      setAviso({ tipo: 'exito', texto: 'Perfil actualizado con éxito.' });
    } catch (err) {
      setAviso({
        tipo: 'error',
        texto: err instanceof Error ? err.message : 'No se pudo actualizar el perfil.',
      });
    } finally {
      setGuardando(false);
    }
  };

  return (
    <section>
      <TituloPanel titulo="Mi Perfil" />
      {aviso && <Mensaje tipo={aviso.tipo}>{aviso.texto}</Mensaje>}

      <div className="rejilla-panel">
        <div className="caja">
          <h2 className="subtitulo">Datos de la cuenta</h2>
          <dl className="perfil__datos" style={{ marginTop: '1rem' }}>
            <dt>Correo electrónico</dt>
            <dd>{perfil.email}</dd>
            <dt>Rol en el sistema</dt>
            <dd>{perfil.roles.join(', ')}</dd>
            <dt>Miembro desde</dt>
            <dd>{formatoFecha(perfil.fechaRegistro)}</dd>
          </dl>
        </div>

        <div className="caja">
          <h2 className="subtitulo">Editar datos personales</h2>
          <form className="formulario" onSubmit={guardar} style={{ marginTop: '1rem' }}>
            <div className="campo">
              <label htmlFor="yo-nombre">Nombre completo *</label>
              <input
                id="yo-nombre"
                name="nombreCompleto"
                type="text"
                required
                maxLength={150}
                value={nombreCompleto}
                onChange={(e) => setNombreCompleto(e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="yo-telefono">Teléfono</label>
              <input
                id="yo-telefono"
                name="telefono"
                type="tel"
                maxLength={30}
                value={telefono}
                onChange={(e) => setTelefono(e.target.value)}
              />
            </div>
            <div className="acciones">
              <button type="submit" className="boton boton--accion" disabled={guardando}>
                {guardando ? 'Guardando…' : 'Guardar cambios'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </section>
  );
}

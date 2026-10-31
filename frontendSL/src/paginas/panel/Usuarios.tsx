import { useState } from 'react';
import { api } from '../../api/cliente';
import type { Rol, Usuario } from '../../api/tipos';
import { useAuth } from '../../auth/contexto';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { Dialogo, FormularioAccion, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';
import { formatoFecha } from '../../utilidades/formato';

const TODOS_LOS_ROLES: Rol[] = ['Superadministrador', 'Administrador', 'Trabajador', 'Cliente'];

// Cuentas del sistema. El Administrador solo ve y maneja Clientes y Trabajadores;
// el Superadministrador maneja todas (la API aplica las mismas reglas).
export function Usuarios() {
  const { perfil, tieneRol } = useAuth();
  const esSuper = tieneRol('Superadministrador');
  const rolesPermitidos: Rol[] = esSuper ? TODOS_LOS_ROLES : ['Trabajador', 'Cliente'];

  const [rol, setRol] = useState('');
  const { datos, cargando, error, recargar } = useDatos<Usuario[]>(
    `/api/usuarios?incluirInactivos=true${rol ? `&rol=${rol}` : ''}`,
  );
  const [creando, setCreando] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);

  const ejecutar = async (llamada: () => Promise<unknown>, exito: string) => {
    try {
      await llamada();
      setAviso({ tipo: 'exito', texto: exito });
      recargar();
    } catch (e) {
      setAviso({ tipo: 'error', texto: e instanceof Error ? e.message : 'No se pudo completar la acción.' });
    }
  };

  return (
    <section>
      <TituloPanel titulo="Usuarios">
        <button type="button" className="boton boton--accion boton--chico" onClick={() => setCreando(true)}>
          + Nuevo usuario
        </button>
      </TituloPanel>

      <div className="filtros" role="group" aria-label="Filtrar por rol">
        {['', ...rolesPermitidos].map((r) => (
          <button key={r || 'todos'} type="button" className="chip" aria-pressed={rol === r} onClick={() => setRol(r)}>
            {r || 'Todos'}
          </button>
        ))}
      </div>

      {aviso && <Mensaje tipo={aviso.tipo}>{aviso.texto}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}

      {datos && (
        <div className="tabla-envoltura">
          <table className="tabla">
            <thead>
              <tr>
                <th scope="col">Nombre</th>
                <th scope="col">Correo</th>
                <th scope="col">Rol</th>
                <th scope="col">Desde</th>
                <th scope="col">Estado</th>
              </tr>
            </thead>
            <tbody>
              {datos.map((u) => {
                const soyYo = u.id === perfil?.id;
                return (
                  <tr key={u.id} className={u.activo ? '' : 'tabla__inactiva'}>
                    <td data-etiqueta="Nombre">
                      <strong>{u.nombreCompleto}</strong>
                      {soyYo && <span className="texto-suave"> (usted)</span>}
                    </td>
                    <td data-etiqueta="Correo">{u.email}</td>
                    <td data-etiqueta="Rol">
                      {soyYo ? (
                        u.rol
                      ) : (
                        <select
                          aria-label={`Rol de ${u.nombreCompleto}`}
                          name={`rol-${u.id}`}
                          className="entrada-media"
                          value={u.rol}
                          onChange={(e) =>
                            ejecutar(
                              () => api.patch(`/api/usuarios/${u.id}/rol`, { rol: e.target.value }),
                              `${u.nombreCompleto} ahora es ${e.target.value}.`,
                            )
                          }
                        >
                          {rolesPermitidos.map((r) => (
                            <option key={r} value={r}>
                              {r}
                            </option>
                          ))}
                        </select>
                      )}
                    </td>
                    <td data-etiqueta="Desde">{formatoFecha(u.fechaRegistro)}</td>
                    <td data-etiqueta="Estado">
                      {soyYo ? (
                        <span className="insignia insignia--ok">Activo</span>
                      ) : (
                        <button
                          type="button"
                          className={`boton boton--mini ${u.activo ? 'boton--borde' : 'boton--accion'}`}
                          onClick={() =>
                            ejecutar(
                              () => api.patch(`/api/usuarios/${u.id}/activo`, { activo: !u.activo }),
                              u.activo ? `Se desactivó a ${u.nombreCompleto}.` : `Se activó a ${u.nombreCompleto}.`,
                            )
                          }
                        >
                          {u.activo ? 'Desactivar' : 'Activar'}
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <Dialogo titulo="Nuevo usuario" abierto={creando} alCerrar={() => setCreando(false)}>
        {creando && (
          <NuevoUsuario
            roles={esSuper ? TODOS_LOS_ROLES : ['Trabajador']}
            alTerminar={(nombre) => {
              setCreando(false);
              setAviso({ tipo: 'exito', texto: `Se creó la cuenta de ${nombre}.` });
              recargar();
            }}
          />
        )}
      </Dialogo>
    </section>
  );
}

function NuevoUsuario({ roles, alTerminar }: { roles: Rol[]; alTerminar: (nombre: string) => void }) {
  const [datos, setDatos] = useState({ nombreCompleto: '', email: '', telefono: '', password: '', rol: roles[0] });
  const cambiar = (campo: keyof typeof datos, valor: string) => setDatos((d) => ({ ...d, [campo]: valor }));

  return (
    <FormularioAccion
      textoBoton="Crear cuenta"
      alEnviar={async () => {
        await api.post('/api/usuarios', {
          nombreCompleto: datos.nombreCompleto.trim(),
          email: datos.email.trim(),
          telefono: datos.telefono.trim() || null,
          password: datos.password,
          rol: datos.rol,
        });
        alTerminar(datos.nombreCompleto.trim());
      }}
    >
      <div className="campo">
        <label htmlFor="u-nombre">Nombre completo *</label>
        <input id="u-nombre" name="nombreCompleto" required maxLength={150} autoComplete="off" value={datos.nombreCompleto} onChange={(e) => cambiar('nombreCompleto', e.target.value)} />
      </div>
      <div className="fila">
        <div className="campo">
          <label htmlFor="u-correo">Correo *</label>
          <input id="u-correo" name="email" type="email" required spellCheck={false} autoComplete="off" value={datos.email} onChange={(e) => cambiar('email', e.target.value)} />
        </div>
        <div className="campo">
          <label htmlFor="u-telefono">Teléfono</label>
          <input id="u-telefono" name="telefono" type="tel" maxLength={30} autoComplete="off" value={datos.telefono} onChange={(e) => cambiar('telefono', e.target.value)} />
        </div>
      </div>
      <div className="fila">
        <div className="campo">
          <label htmlFor="u-password">Contraseña temporal *</label>
          <input id="u-password" name="password" type="password" required minLength={8} autoComplete="new-password" value={datos.password} onChange={(e) => cambiar('password', e.target.value)} />
          <p className="campo__ayuda">Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.</p>
        </div>
        <div className="campo">
          <label htmlFor="u-rol">Rol *</label>
          <select id="u-rol" name="rol" value={datos.rol} onChange={(e) => cambiar('rol', e.target.value)}>
            {roles.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
        </div>
      </div>
    </FormularioAccion>
  );
}

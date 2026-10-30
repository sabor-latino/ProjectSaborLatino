import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, ErrorApi } from '../api/cliente';
import type { Paquete, Perfil, Solicitud, SolicitudCrear } from '../api/tipos';
import { useAuth } from '../auth/contexto';
import { Mensaje } from '../componentes/Estado';
import { IconoCheck } from '../componentes/Iconos';
import { useDatos } from '../hooks/useDatos';
import { ahoraParaInput, formatoColones, formatoFechaHora } from '../utilidades/formato';
import { validacionAccesible } from '../utilidades/formularios';
import { FOTO_COTIZAR } from '../utilidades/fotos';

const TIPOS_EVENTO = ['Boda', 'Quince años', 'Cumpleaños', 'Fiesta de empresa', 'Graduación', 'Aniversario'];

export function SolicitarCotizacion() {
  const { perfil, cargando } = useAuth();
  const [enviada, setEnviada] = useState<Solicitud | null>(null);

  return (
    <section className="seccion banda" aria-labelledby="titulo-cotizar">
      <img className="banda__foto" src={FOTO_COTIZAR} alt="" width={1800} height={1200} />
      <div className="contenedor banda__grid">
        <div className="encabezado encabezado--sin-margen">
          <p className="antetitulo">Solicitar cotización</p>
          <h1 id="titulo-cotizar" className="titulo-seccion">
            Cuéntenos de su evento
          </h1>
          <p className="intro">Le enviamos una cotización en colones. Pedirla no tiene costo ni compromiso.</p>
          <ul className="ventajas">
            <li>
              <IconoCheck />
              Precio claro con el equipo incluido
            </li>
            <li>
              <IconoCheck />
              Montaje y prueba de sonido antes de la fiesta
            </li>
            <li>
              <IconoCheck />
              Si tiene cuenta, la sigue desde “Mi cuenta”
            </li>
          </ul>
        </div>

        {enviada ? (
          <SolicitudEnviada solicitud={enviada} alReiniciar={() => setEnviada(null)} />
        ) : (
          // La "key" vuelve a crear el formulario cuando termina de cargar la sesión,
          // así se rellenan los datos del cliente que inició sesión
          !cargando && <FormularioSolicitud key={perfil?.id ?? 'visitante'} perfil={perfil} alEnviar={setEnviada} />
        )}
      </div>
    </section>
  );
}

function SolicitudEnviada({ solicitud, alReiniciar }: { solicitud: Solicitud; alReiniciar: () => void }) {
  const titulo = useRef<HTMLHeadingElement>(null);

  // Lleva el foco al mensaje para que los lectores de pantalla lo anuncien
  useEffect(() => {
    titulo.current?.focus();
  }, []);

  return (
    <div className="formulario">
      <Mensaje tipo="exito">
        <h2 ref={titulo} tabIndex={-1} className="mensaje__titulo">
          ¡Recibimos su solicitud!
        </h2>
        <p>
          Solicitud #{solicitud.id} para el {formatoFechaHora(solicitud.fechaEvento)} en {solicitud.lugar}. Le
          escribiremos a <strong>{solicitud.correoContacto}</strong> con la cotización.
        </p>
      </Mensaje>
      <div className="acciones">
        <button type="button" className="boton boton--accion" onClick={alReiniciar}>
          Enviar otra solicitud
        </button>
        <Link to="/paquetes" className="boton boton--borde">
          Ver paquetes
        </Link>
      </div>
    </div>
  );
}

interface PropsFormulario {
  perfil: Perfil | null;
  alEnviar: (solicitud: Solicitud) => void;
}

function FormularioSolicitud({ perfil, alEnviar }: PropsFormulario) {
  const [parametros] = useSearchParams();
  const paquetes = useDatos<Paquete[]>('/api/paquetes');
  const [minimo] = useState(ahoraParaInput);
  const cajaErrores = useRef<HTMLDivElement>(null);

  const [datos, setDatos] = useState({
    nombreContacto: perfil?.nombreCompleto ?? '',
    correoContacto: perfil?.email ?? '',
    telefonoContacto: perfil?.telefono ?? '',
    paqueteId: parametros.get('paquete') ?? '',
    tipoEvento: '',
    fechaEvento: '',
    lugar: '',
    cantidadInvitados: '',
    comentarios: '',
  });
  const [enviando, setEnviando] = useState(false);
  const [errores, setErrores] = useState<string[]>([]);

  // Si la API rechaza el envío, el foco va a la lista de errores
  useEffect(() => {
    if (errores.length > 0) {
      cajaErrores.current?.focus();
    }
  }, [errores]);

  const cambiar = (campo: keyof typeof datos, valor: string) => setDatos((d) => ({ ...d, [campo]: valor }));

  // Solo se ejecuta si el navegador ya validó los campos (required, type="email", min…).
  // Si falta algo, el navegador lleva el foco al primer campo con error.
  const enviar = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setEnviando(true);
    setErrores([]);

    const cuerpo: SolicitudCrear = {
      nombreContacto: datos.nombreContacto.trim(),
      correoContacto: datos.correoContacto.trim(),
      telefonoContacto: datos.telefonoContacto.trim() || undefined,
      paqueteId: datos.paqueteId ? Number(datos.paqueteId) : undefined,
      tipoEvento: datos.tipoEvento.trim(),
      fechaEvento: datos.fechaEvento,
      lugar: datos.lugar.trim(),
      cantidadInvitados: datos.cantidadInvitados ? Number(datos.cantidadInvitados) : undefined,
      comentarios: datos.comentarios.trim() || undefined,
    };

    try {
      // Si hay sesión, el cliente de la API manda el token y la solicitud queda en la cuenta
      alEnviar(await api.post<Solicitud>('/api/solicitudes', cuerpo));
    } catch (error) {
      setErrores(error instanceof ErrorApi ? error.mensajes : ['No se pudo enviar la solicitud. Intente de nuevo.']);
      setEnviando(false);
    }
  };

  return (
    <form className="formulario" onSubmit={enviar}>
      <h2 className="formulario__titulo">Datos del evento</h2>
      {perfil && <Mensaje>Está enviando la solicitud con su cuenta; la podrá seguir desde “Mi cuenta”.</Mensaje>}

      <div className="campo">
        <label htmlFor="nombre">Nombre completo</label>
        <input
          id="nombre"
          name="nombreContacto"
          required
          maxLength={150}
          autoComplete="name"
          aria-errormessage="nombre-error"
          {...validacionAccesible}
          value={datos.nombreContacto}
          onChange={(e) => cambiar('nombreContacto', e.target.value)}
        />
        <p className="campo__error" id="nombre-error">
          Escriba su nombre.
        </p>
      </div>

      <div className="fila">
        <div className="campo">
          <label htmlFor="correo">Correo electrónico</label>
          <input
            id="correo"
            name="correoContacto"
            type="email"
            required
            maxLength={256}
            autoComplete="email"
            spellCheck={false}
            aria-errormessage="correo-error"
            {...validacionAccesible}
            value={datos.correoContacto}
            onChange={(e) => cambiar('correoContacto', e.target.value)}
          />
          <p className="campo__error" id="correo-error">
            Escriba un correo válido, por ejemplo nombre@correo.com.
          </p>
        </div>

        <div className="campo">
          <label htmlFor="telefono">Teléfono (opcional)</label>
          <input
            id="telefono"
            name="telefonoContacto"
            type="tel"
            inputMode="tel"
            maxLength={30}
            autoComplete="tel"
            value={datos.telefonoContacto}
            onChange={(e) => cambiar('telefonoContacto', e.target.value)}
          />
        </div>
      </div>

      <div className="campo">
        <label htmlFor="paquete">Paquete que le interesa</label>
        <select
          id="paquete"
          name="paqueteId"
          value={datos.paqueteId}
          onChange={(e) => cambiar('paqueteId', e.target.value)}
        >
          <option value="">Todavía no sé / quiero asesoría</option>
          {paquetes.datos?.map((p) => (
            <option key={p.id} value={p.id}>
              {p.nombre} — desde {formatoColones(p.precioBase)}
            </option>
          ))}
        </select>
      </div>

      <div className="fila">
        <div className="campo">
          <label htmlFor="tipo">Tipo de evento</label>
          <input
            id="tipo"
            name="tipoEvento"
            required
            maxLength={100}
            list="tipos-evento"
            autoComplete="off"
            placeholder="Boda, quince años…"
            aria-errormessage="tipo-error"
            {...validacionAccesible}
            value={datos.tipoEvento}
            onChange={(e) => cambiar('tipoEvento', e.target.value)}
          />
          <datalist id="tipos-evento">
            {TIPOS_EVENTO.map((t) => (
              <option key={t} value={t} />
            ))}
          </datalist>
          <p className="campo__error" id="tipo-error">
            Indique el tipo de evento.
          </p>
        </div>

        <div className="campo">
          <label htmlFor="fecha">Fecha y hora</label>
          <input
            id="fecha"
            name="fechaEvento"
            type="datetime-local"
            required
            min={minimo}
            aria-errormessage="fecha-error"
            {...validacionAccesible}
            value={datos.fechaEvento}
            onChange={(e) => cambiar('fechaEvento', e.target.value)}
          />
          <p className="campo__error" id="fecha-error">
            Elija una fecha futura.
          </p>
        </div>
      </div>

      <div className="fila">
        <div className="campo">
          <label htmlFor="lugar">Lugar</label>
          <input
            id="lugar"
            name="lugar"
            required
            maxLength={300}
            autoComplete="off"
            placeholder="Salón comunal, Heredia…"
            aria-errormessage="lugar-error"
            {...validacionAccesible}
            value={datos.lugar}
            onChange={(e) => cambiar('lugar', e.target.value)}
          />
          <p className="campo__error" id="lugar-error">
            Indique dónde será el evento.
          </p>
        </div>

        <div className="campo">
          <label htmlFor="invitados">Invitados (opcional)</label>
          <input
            id="invitados"
            name="cantidadInvitados"
            type="number"
            min={1}
            max={100000}
            inputMode="numeric"
            autoComplete="off"
            aria-errormessage="invitados-error"
            {...validacionAccesible}
            value={datos.cantidadInvitados}
            onChange={(e) => cambiar('cantidadInvitados', e.target.value)}
          />
          <p className="campo__error" id="invitados-error">
            Debe ser un número mayor que 0.
          </p>
        </div>
      </div>

      <div className="campo">
        <label htmlFor="comentarios">Comentarios (opcional)</label>
        <textarea
          id="comentarios"
          name="comentarios"
          rows={4}
          maxLength={2000}
          autoComplete="off"
          placeholder="Canciones especiales, horario de montaje, lo que debamos saber…"
          value={datos.comentarios}
          onChange={(e) => cambiar('comentarios', e.target.value)}
        />
      </div>

      {errores.length > 0 && (
        <div ref={cajaErrores} tabIndex={-1}>
          <Mensaje tipo="error">
            {errores.map((m) => (
              <p key={m}>{m}</p>
            ))}
          </Mensaje>
        </div>
      )}

      <button type="submit" className="boton boton--accion boton--ancho" disabled={enviando}>
        {enviando ? 'Enviando…' : 'Enviar solicitud'}
      </button>
    </form>
  );
}

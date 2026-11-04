import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, ErrorApi, obtenerToken } from '../api/cliente';
import type { Paquete, Perfil, PromocionTemporada, Solicitud, SolicitudCrear } from '../api/tipos';
import { useAuth } from '../auth/contexto';
import { Cargando, Mensaje } from '../componentes/Estado';
import { IconoCheck } from '../componentes/Iconos';
import { useDatos } from '../hooks/useDatos';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { ahoraParaInput, formatoColones, formatoFechaHora } from '../utilidades/formato';
import { validacionAccesible } from '../utilidades/formularios';
import { FOTO_COTIZAR } from '../utilidades/fotos';
import { crearMensajeWhatsApp, crearUrlWhatsApp, navegacionWhatsApp } from '../utilidades/whatsapp';
import { guardarSolicitudPendiente, leerSolicitudPendiente, limpiarSolicitudPendiente, type IntentoSolicitud } from '../utilidades/solicitudPendiente';

const TIPOS_EVENTO_ES = ['Boda', 'Quince años', 'Cumpleaños', 'Fiesta de empresa', 'Graduación', 'Aniversario'];
const TIPOS_EVENTO_EN = ['Wedding', 'Sweet 15', 'Birthday', 'Corporate event', 'Graduation', 'Anniversary'];

export function SolicitarCotizacion() {
  const { perfil, cargando } = useAuth();
  if (cargando) return <Cargando />;
  return <SolicitudDisponible key={perfil?.id ?? 'visitante'} perfil={perfil} />;
}

function SolicitudDisponible({ perfil }: { perfil: Perfil | null }) {
  const { t } = usePreferencias();
  const actor = perfil?.id ?? 'visitante';
  const [recuperacion] = useState(() => leerSolicitudPendiente(actor));
  const [enviada, setEnviada] = useState<Solicitud | null>(recuperacion?.solicitud ?? null);
  const [abrirAutomaticamente, setAbrirAutomaticamente] = useState(false);
  const [sinRecuperacion, setSinRecuperacion] = useState(false);
  const [formulario, setFormulario] = useState(0);

  const guardada = (solicitud: Solicitud) => {
    const persistida = guardarSolicitudPendiente(actor, { solicitud });
    setSinRecuperacion(!persistida);
    setAbrirAutomaticamente(persistida);
    setEnviada(solicitud);
  };
  const reiniciar = () => {
    limpiarSolicitudPendiente();
    setAbrirAutomaticamente(false);
    setSinRecuperacion(false);
    setEnviada(null);
    setFormulario((valor) => valor + 1);
  };

  return (
    <section className="seccion banda solicitud-cotizacion" aria-labelledby="titulo-cotizar">
      <img className="banda__foto" src={FOTO_COTIZAR} alt="" width={1800} height={1200} />
      <div className="contenedor banda__grid">
        <div className="encabezado encabezado--sin-margen">
          <p className="antetitulo">{t('Solicitar cotización', 'Request a quote')}</p>
          <h1 id="titulo-cotizar" className="titulo-seccion">
            {t('Cuéntenos sobre su celebración', 'Tell us about your celebration')}
          </h1>
          <p className="intro">
            {t(
              'Guarde los datos de su evento y conversemos por WhatsApp. Solicitar una cotización no tiene costo ni compromiso.',
              'Save your event details and continue on WhatsApp. Requesting a quote is free and without obligation.',
            )}
          </p>
          <ul className="ventajas">
            <li>
              <IconoCheck />
              {t('Presupuesto claro con equipo de sonido e iluminación incluidos', 'Clear pricing with sound and lighting equipment included')}
            </li>
            <li>
              <IconoCheck />
              {t('Montaje anticipado y prueba acústica antes de iniciar el evento', 'Early setup and acoustic soundcheck before your event begins')}
            </li>
            <li>
              <IconoCheck />
              {t(
                'Su solicitud queda registrada y continuamos la conversación por WhatsApp',
                'Your request is saved and we continue the conversation on WhatsApp',
              )}
            </li>
          </ul>
        </div>

        {enviada ? (
          <SolicitudEnviada solicitud={enviada} alReiniciar={reiniciar} autoAbrir={abrirAutomaticamente} sinRecuperacion={sinRecuperacion} />
        ) : (
          <FormularioSolicitud key={formulario} perfil={perfil} actor={actor} enfocar={formulario > 0} intentoInicial={formulario === 0 ? recuperacion?.intento : undefined} alEnviar={guardada} />
        )}
      </div>
    </section>
  );
}

function SolicitudEnviada({ solicitud, alReiniciar, autoAbrir, sinRecuperacion }: {
  solicitud: Solicitud; alReiniciar: () => void; autoAbrir: boolean; sinRecuperacion: boolean;
}) {
  const { idioma, t } = usePreferencias();
  const titulo = useRef<HTMLHeadingElement>(null);
  const aperturaIniciada = useRef(false);
  const [aviso, setAviso] = useState('');
  const mensaje = crearMensajeWhatsApp(solicitud, idioma);
  const url = crearUrlWhatsApp(mensaje);

  useEffect(() => {
    titulo.current?.focus();
    if (autoAbrir && !aperturaIniciada.current) {
      aperturaIniciada.current = true;
      try {
        navegacionWhatsApp.abrir(url);
      } catch {
        // La solicitud está guardada aunque el navegador no abra la aplicación.
      }
    }
  }, [url, autoAbrir]);

  const copiar = async () => {
    try {
      await navigator.clipboard.writeText(mensaje);
      setAviso(t('Resumen copiado.', 'Summary copied.'));
    } catch {
      setAviso(t('No se pudo copiar automáticamente. Seleccione el resumen para copiarlo.', 'Could not copy automatically. Select the summary to copy it.'));
    }
  };

  return (
    <div className="formulario formulario--cotizacion confirmacion-solicitud">
      <Mensaje tipo="exito">
        <h2 ref={titulo} tabIndex={-1} className="mensaje__titulo">
          {t('Solicitud guardada', 'Request saved')}
        </h2>
        <p>
          <strong>{t('Solicitud', 'Request')} #{solicitud.id}</strong> ({formatoFechaHora(solicitud.fechaEvento)} · {solicitud.lugar}).{' '}
          {t('Complete el envío en WhatsApp pulsando Enviar. Abrir el chat no confirma que el mensaje haya sido enviado.', 'Complete the submission in WhatsApp by pressing Send. Opening the chat does not confirm that the message was sent.')}
        </p>
      </Mensaje>
      {sinRecuperacion && <Mensaje>{t('El navegador no permite recuperar esta pantalla al volver. Guarde la referencia antes de abrir WhatsApp.', 'Your browser cannot restore this screen when you return. Save the reference before opening WhatsApp.')}</Mensaje>}
      <div className="campo">
        <label htmlFor="resumen-whatsapp">{t('Resumen para WhatsApp', 'WhatsApp summary')}</label>
        <textarea id="resumen-whatsapp" rows={9} readOnly value={mensaje} />
      </div>
      {aviso && <p role="status" className="campo__ayuda">{aviso}</p>}
      <div className="acciones confirmacion-solicitud__principales">
        <a className="boton boton--accion" href={url}>{t('Reabrir WhatsApp', 'Reopen WhatsApp')}</a>
        <button type="button" className="boton boton--borde" onClick={copiar}>
          {t('Copiar resumen', 'Copy summary')}
        </button>
      </div>
      <div className="acciones confirmacion-solicitud__secundarias">
        <button type="button" className="boton boton--texto" onClick={alReiniciar}>
          {t('Nueva solicitud', 'New request')}
        </button>
        <Link to="/paquetes" className="boton boton--texto">
          {t('Ver paquetes', 'View packages')}
        </Link>
      </div>
    </div>
  );
}

interface PropsFormulario {
  perfil: Perfil | null;
  actor: string;
  enfocar: boolean;
  intentoInicial?: IntentoSolicitud;
  alEnviar: (solicitud: Solicitud) => void;
}

function FormularioSolicitud({ perfil, actor, enfocar, intentoInicial, alEnviar }: PropsFormulario) {
  const { idioma, t } = usePreferencias();
  const [parametros] = useSearchParams();
  const paquetes = useDatos<Paquete[]>('/api/paquetes');
  const promociones = useDatos<PromocionTemporada[]>('/api/promociones');
  const [minimo] = useState(ahoraParaInput);
  const cajaErrores = useRef<HTMLDivElement>(null);

  const promocionParamId = parametros.get('promocion');

  const [datos, setDatos] = useState({
    nombreContacto: intentoInicial?.cuerpo.nombreContacto ?? perfil?.nombreCompleto ?? '',
    correoContacto: intentoInicial?.cuerpo.correoContacto ?? perfil?.email ?? '',
    telefonoContacto: intentoInicial?.cuerpo.telefonoContacto ?? perfil?.telefono ?? '',
    paqueteId: intentoInicial?.cuerpo.paqueteId?.toString() ?? parametros.get('paquete') ?? '',
    tipoEvento: intentoInicial?.cuerpo.tipoEvento ?? '',
    fechaEvento: intentoInicial?.cuerpo.fechaEvento ?? '',
    lugar: intentoInicial?.cuerpo.lugar ?? '',
    cantidadInvitados: intentoInicial?.cuerpo.cantidadInvitados?.toString() ?? '',
    comentarios: intentoInicial?.cuerpo.comentarios ?? '',
  });
  const [enviando, setEnviando] = useState(false);
  const [errores, setErrores] = useState<string[]>([]);
  const guardando = useRef(false);
  const intento = useRef<IntentoSolicitud | null>(intentoInicial ?? null);

  useEffect(() => {
    if (errores.length > 0) {
      cajaErrores.current?.focus();
    }
  }, [errores]);

  const listaPromos = Array.isArray(promociones.datos)
    ? promociones.datos.filter((pr) => pr.activo && pr.vigente !== false)
    : [];

  const promoAplicable = (() => {
    if (promocionParamId) {
      const porUrl = listaPromos.find((pr) => String(pr.id) === promocionParamId);
      if (porUrl) return porUrl;
    }
    if (datos.paqueteId) {
      const pkId = Number(datos.paqueteId);
      return listaPromos.find((pr) => pr.paquetes?.some((pk) => pk.paqueteId === pkId));
    }
    return undefined;
  })();

  const cambiar = (campo: keyof typeof datos, valor: string) => setDatos((d) => ({ ...d, [campo]: valor }));

  const enviar = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (guardando.current) return;
    guardando.current = true;
    setEnviando(true);
    setErrores([]);

    try {
      let comentariosFinales = datos.comentarios.trim();
      if (promoAplicable) {
        const notaPromo = `[Promoción de temporada: ${promoAplicable.nombre} (-${promoAplicable.porcentajeDescuento}%)${
          promoAplicable.codigoPromocional ? ` · Código: ${promoAplicable.codigoPromocional}` : ''
        }]`;
        if (!comentariosFinales.includes('[Promoción de temporada:')) {
          comentariosFinales = comentariosFinales ? `${notaPromo} ${comentariosFinales}` : notaPromo;
        }
      }

      const cuerpo: SolicitudCrear = {
        nombreContacto: datos.nombreContacto.trim(),
        correoContacto: datos.correoContacto.trim() || undefined,
        telefonoContacto: datos.telefonoContacto.trim() || undefined,
        paqueteId: datos.paqueteId ? Number(datos.paqueteId) : undefined,
        tipoEvento: datos.tipoEvento.trim(),
        fechaEvento: datos.fechaEvento,
        lugar: datos.lugar.trim(),
        cantidadInvitados: datos.cantidadInvitados ? Number(datos.cantidadInvitados) : undefined,
        comentarios: comentariosFinales || undefined,
      };
      const huella = JSON.stringify(cuerpo);
      if (intento.current?.huella !== huella) {
        intento.current = { huella, clave: crypto.randomUUID(), cuerpo };
      }
      cuerpo.claveIdempotencia = intento.current.clave;

      if (perfil && !obtenerToken()) {
        throw new ErrorApi(401, [
          t(
            'Su sesión venció. Inicie sesión nuevamente antes de enviar la solicitud para guardarla en su cuenta.',
            'Your session has expired. Please sign in again before submitting so the request is saved to your account.',
          ),
        ]);
      }

      guardarSolicitudPendiente(actor, { intento: intento.current });
      alEnviar(await api.post<Solicitud>('/api/solicitudes', cuerpo));
    } catch (error) {
      setErrores(
        error instanceof ErrorApi
          ? error.mensajes
          : [t('No se pudo guardar la solicitud. Intente de nuevo.', 'Could not save the request. Please try again.')],
      );
      setEnviando(false);
    } finally {
      guardando.current = false;
    }
  };

  const listaPaquetes = Array.isArray(paquetes.datos) ? paquetes.datos : [];
  const tiposEvento = idioma === 'en' ? TIPOS_EVENTO_EN : TIPOS_EVENTO_ES;

  return (
    <form className="formulario formulario--cotizacion" onSubmit={enviar} aria-labelledby="titulo-solicitud" aria-describedby="ayuda-solicitud">
      <div className="formulario__cabecera">
        <h2 id="titulo-solicitud" className="formulario__titulo">{t('Su solicitud', 'Your request')}</h2>
        <p id="ayuda-solicitud" className="campo__ayuda">{t(
          'Guardamos su solicitud antes de abrir WhatsApp. Allí podrá revisar el mensaje y pulsar Enviar.',
          'We save your request before opening WhatsApp. You can then review the message and press Send.',
        )}</p>
      </div>
      {perfil && (
        <Mensaje>
          {t(
            'La solicitud quedará vinculada a su cuenta y el equipo podrá darle seguimiento.',
            'The request will be linked to your account so our team can follow up.',
          )}
        </Mensaje>
      )}

      {promoAplicable && (
        <Mensaje tipo="exito">
          <strong>
            {idioma === 'en' && promoAplicable.nombreEn ? promoAplicable.nombreEn : promoAplicable.nombre} (-
            {promoAplicable.porcentajeDescuento}%)
          </strong>
          {promoAplicable.codigoPromocional ? ` · ${t('Código:', 'Code:')} ${promoAplicable.codigoPromocional}` : ''} —{' '}
          {t(
            'Esta promoción de temporada se incluirá automáticamente en su solicitud de cotización.',
            'This seasonal promotion will be automatically included in your quote request.',
          )}
        </Mensaje>
      )}

      <fieldset className="grupo-campos">
        <legend>{t('Contacto', 'Contact')}</legend>
      <div className="campo">
        <label htmlFor="nombre">{t('Nombre completo', 'Full name')}</label>
        <input
          id="nombre"
          name="nombreContacto"
          required
          maxLength={150}
          autoComplete="name"
          autoFocus={enfocar}
          aria-errormessage="nombre-error"
          {...validacionAccesible}
          value={datos.nombreContacto}
          onChange={(e) => cambiar('nombreContacto', e.target.value)}
        />
        <p className="campo__error" id="nombre-error">
          {t('Escriba su nombre completo.', 'Please enter your full name.')}
        </p>
      </div>

      <div className="fila">
        <div className="campo">
          <label htmlFor="correo">{t('Correo electrónico (opcional)', 'Email address (optional)')}</label>
          <input
            id="correo"
            name="correoContacto"
            type="email"
            maxLength={256}
            autoComplete="email"
            spellCheck={false}
            aria-errormessage="correo-error"
            {...validacionAccesible}
            value={datos.correoContacto}
            onChange={(e) => cambiar('correoContacto', e.target.value)}
          />
          <p className="campo__error" id="correo-error">
            {t(
              'Escriba un correo electrónico válido, por ejemplo nombre@correo.com.',
              'Enter a valid email address, for example name@domain.com.',
            )}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="telefono">{t('Teléfono de contacto', 'Contact phone number')}</label>
          <input
            id="telefono"
            name="telefonoContacto"
            type="tel"
            required
            pattern={'[+0-9\\(\\) .\\-]{7,30}'}
            inputMode="tel"
            maxLength={30}
            autoComplete="tel"
            aria-describedby="telefono-ayuda"
            aria-errormessage="telefono-error"
            {...validacionAccesible}
            value={datos.telefonoContacto}
            onChange={(e) => cambiar('telefonoContacto', e.target.value)}
          />
          <p id="telefono-ayuda" className="campo__ayuda">{t('Para contactarle si no completa el mensaje.', 'So we can contact you if you do not send the message.')}</p>
          <p id="telefono-error" className="campo__error">{t('Revise el teléfono: use números y, si corresponde, el código de país.', 'Check the phone number: use digits and the country code when needed.')}</p>
        </div>
      </div>
      </fieldset>

      <fieldset className="grupo-campos">
        <legend>{t('El evento', 'The event')}</legend>
      <div className="campo">
        <label htmlFor="paquete">{t('Paquete que le interesa', 'Package of interest')}</label>
        <select
          id="paquete"
          name="paqueteId"
          value={datos.paqueteId}
          onChange={(e) => cambiar('paqueteId', e.target.value)}
        >
          <option value="">{t('Todavía no sé / deseo asesoría técnica', 'Not sure yet / I would like advice')}</option>
          {listaPaquetes.map((p) => {
            const nombrePk = idioma === 'en' && p.nombreEn ? p.nombreEn : p.nombre;
            return (
              <option key={p.id} value={p.id}>
                {nombrePk} — {t('desde', 'from')} {formatoColones(p.precioBase)}
              </option>
            );
          })}
        </select>
      </div>

      <div className="fila">
        <div className="campo">
          <label htmlFor="tipo">{t('Tipo de evento', 'Event type')}</label>
          <input
            id="tipo"
            name="tipoEvento"
            required
            maxLength={100}
            list="tipos-evento"
            autoComplete="off"
            placeholder={t('Boda, quince años, corporativo…', 'Wedding, sweet 15, corporate…')}
            aria-errormessage="tipo-error"
            {...validacionAccesible}
            value={datos.tipoEvento}
            onChange={(e) => cambiar('tipoEvento', e.target.value)}
          />
          <datalist id="tipos-evento">
            {tiposEvento.map((tipo) => (
              <option key={tipo} value={tipo} />
            ))}
          </datalist>
          <p className="campo__error" id="tipo-error">
            {t('Indique el tipo de evento.', 'Please specify the event type.')}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="fecha">{t('Fecha y hora', 'Date and time')}</label>
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
            {t('Seleccione una fecha futura.', 'Please choose a future date.')}
          </p>
        </div>
      </div>

      <div className="fila">
        <div className="campo">
          <label htmlFor="lugar">{t('Lugar del evento', 'Event venue / location')}</label>
          <input
            id="lugar"
            name="lugar"
            required
            maxLength={300}
            autoComplete="off"
            placeholder={t('Salón de eventos, hotel o residencia, provincia…', 'Event hall, hotel or residence, province…')}
            aria-errormessage="lugar-error"
            {...validacionAccesible}
            value={datos.lugar}
            onChange={(e) => cambiar('lugar', e.target.value)}
          />
          <p className="campo__error" id="lugar-error">
            {t('Indique dónde se realizará el evento.', 'Please enter the event location.')}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="invitados">{t('Invitados estimados (opcional)', 'Estimated guests (optional)')}</label>
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
            {t('Debe ser un número mayor que 0.', 'Must be a number greater than 0.')}
          </p>
        </div>
      </div>
      {/jard[ií]n|aire libre|playa|exterior|intemperie|finca|quinta|piscina/i.test(`${datos.lugar}`) && (
        <Mensaje tipo="info">
          <p>
            <strong>{t('⛺ Requisito obligatorio para exteriores:', '⛺ Mandatory requirement for outdoors:')}</strong>{' '}
            {t(
              'Para eventos al aire libre o jardín, se requiere de forma obligatoria tarima techada e impermeable para salvaguardar los equipos electrónicos de sonido e iluminación.',
              'For outdoor or garden events, a covered and waterproof stage is strictly required to safeguard electronic sound and lighting equipment.'
            )}
          </p>
        </Mensaje>
      )}
      </fieldset>

      <fieldset className="grupo-campos">
        <legend>{t('Detalles adicionales', 'Additional details')}</legend>
      <div className="campo">
        <label htmlFor="comentarios">{t('Comentarios (opcional)', 'Comments (optional)')}</label>
        <textarea
          id="comentarios"
          name="comentarios"
          rows={4}
          maxLength={2000}
          autoComplete="off"
          aria-describedby="comentarios-ayuda"
          placeholder={t(
            'Géneros musicales preferidos, horario de montaje, condiciones del salón o detalles importantes…',
            'Preferred music genres, setup schedule, venue conditions, or important details…',
          )}
          value={datos.comentarios}
          onChange={(e) => cambiar('comentarios', e.target.value)}
        />
        <p id="comentarios-ayuda" className="campo__ayuda">{t('Puede incluir música preferida, montaje o necesidades del lugar.', 'You can include music preferences, setup or venue requirements.')}</p>
      </div>
      </fieldset>

      {errores.length > 0 && (
        <div ref={cajaErrores} tabIndex={-1} role="region" className="formulario__errores" aria-labelledby="titulo-errores-solicitud">
          <Mensaje tipo="error">
            <h3 id="titulo-errores-solicitud" className="formulario__error-titulo">{t('No pudimos guardar la solicitud', 'We could not save your request')}</h3>
            {errores.map((m) => (
              <p key={m}>{m}</p>
            ))}
          </Mensaje>
        </div>
      )}

      <button type="submit" className="boton boton--accion boton--ancho" disabled={enviando}>
        {enviando ? t('Guardando…', 'Saving…') : t('Guardar y abrir WhatsApp', 'Save and open WhatsApp')}
      </button>
    </form>
  );
}

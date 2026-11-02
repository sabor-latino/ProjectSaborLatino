import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, ErrorApi, obtenerToken } from '../api/cliente';
import type { Paquete, Perfil, PromocionTemporada, Solicitud, SolicitudCrear } from '../api/tipos';
import { useAuth } from '../auth/contexto';
import { Mensaje } from '../componentes/Estado';
import { IconoCheck } from '../componentes/Iconos';
import { useDatos } from '../hooks/useDatos';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { ahoraParaInput, formatoColones, formatoFechaHora } from '../utilidades/formato';
import { validacionAccesible } from '../utilidades/formularios';
import { FOTO_COTIZAR } from '../utilidades/fotos';

const TIPOS_EVENTO_ES = ['Boda', 'Quince años', 'Cumpleaños', 'Fiesta de empresa', 'Graduación', 'Aniversario'];
const TIPOS_EVENTO_EN = ['Wedding', 'Sweet 15', 'Birthday', 'Corporate event', 'Graduation', 'Anniversary'];

export function SolicitarCotizacion() {
  const { perfil, cargando } = useAuth();
  const { t } = usePreferencias();
  const [enviada, setEnviada] = useState<Solicitud | null>(null);

  return (
    <section className="seccion banda" aria-labelledby="titulo-cotizar">
      <img className="banda__foto" src={FOTO_COTIZAR} alt="" width={1800} height={1200} />
      <div className="contenedor banda__grid">
        <div className="encabezado encabezado--sin-margen">
          <p className="antetitulo">{t('Solicitar cotización', 'Request a quote')}</p>
          <h1 id="titulo-cotizar" className="titulo-seccion">
            {t('Cuéntenos sobre su celebración', 'Tell us about your celebration')}
          </h1>
          <p className="intro">
            {t(
              'Le enviaremos una cotización formal en colones costarricenses con el desglose técnico completo. Solicitarla no tiene ningún costo ni compromiso.',
              'We will send you a formal quote in Costa Rican colones with the full technical breakdown. Requesting it is free and without obligation.',
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
                'Atención directa y propuesta formal enviada a su correo electrónico',
                'Direct assistance and formal quote sent to your email address',
              )}
            </li>
          </ul>
        </div>

        {enviada ? (
          <SolicitudEnviada solicitud={enviada} alReiniciar={() => setEnviada(null)} />
        ) : (
          !cargando && <FormularioSolicitud key={perfil?.id ?? 'visitante'} perfil={perfil} alEnviar={setEnviada} />
        )}
      </div>
    </section>
  );
}

function SolicitudEnviada({ solicitud, alReiniciar }: { solicitud: Solicitud; alReiniciar: () => void }) {
  const { t } = usePreferencias();
  const titulo = useRef<HTMLHeadingElement>(null);

  useEffect(() => {
    titulo.current?.focus();
  }, []);

  return (
    <div className="formulario">
      <Mensaje tipo="exito">
        <h2 ref={titulo} tabIndex={-1} className="mensaje__titulo">
          {t('¡Recibimos su solicitud!', 'We received your request!')}
        </h2>
        <p>
          {t('Solicitud', 'Request')} #{solicitud.id} ({formatoFechaHora(solicitud.fechaEvento)} · {solicitud.lugar}).{' '}
          {t('Le enviaremos la propuesta formal a', 'We will send your formal quote to')}{' '}
          <strong>{solicitud.correoContacto}</strong>.
        </p>
      </Mensaje>
      <div className="acciones">
        <button type="button" className="boton boton--accion" onClick={alReiniciar}>
          {t('Enviar otra solicitud', 'Submit another request')}
        </button>
        <Link to="/paquetes" className="boton boton--borde">
          {t('Ver paquetes', 'View packages')}
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
  const { idioma, t } = usePreferencias();
  const [parametros] = useSearchParams();
  const paquetes = useDatos<Paquete[]>('/api/paquetes');
  const promociones = useDatos<PromocionTemporada[]>('/api/promociones');
  const [minimo] = useState(ahoraParaInput);
  const cajaErrores = useRef<HTMLDivElement>(null);

  const promocionParamId = parametros.get('promocion');

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
    setEnviando(true);
    setErrores([]);

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
      correoContacto: datos.correoContacto.trim(),
      telefonoContacto: datos.telefonoContacto.trim() || undefined,
      paqueteId: datos.paqueteId ? Number(datos.paqueteId) : undefined,
      tipoEvento: datos.tipoEvento.trim(),
      fechaEvento: datos.fechaEvento,
      lugar: datos.lugar.trim(),
      cantidadInvitados: datos.cantidadInvitados ? Number(datos.cantidadInvitados) : undefined,
      comentarios: comentariosFinales || undefined,
    };

    try {
      if (perfil && !obtenerToken()) {
        throw new ErrorApi(401, [
          t(
            'Su sesión venció. Inicie sesión nuevamente antes de enviar la solicitud para guardarla en su cuenta.',
            'Your session has expired. Please sign in again before submitting so the request is saved to your account.',
          ),
        ]);
      }

      alEnviar(await api.post<Solicitud>('/api/solicitudes', cuerpo));
    } catch (error) {
      setErrores(
        error instanceof ErrorApi
          ? error.mensajes
          : [t('No se pudo enviar la solicitud. Intente de nuevo.', 'Could not submit the request. Please try again.')],
      );
      setEnviando(false);
    }
  };

  const listaPaquetes = Array.isArray(paquetes.datos) ? paquetes.datos : [];
  const tiposEvento = idioma === 'en' ? TIPOS_EVENTO_EN : TIPOS_EVENTO_ES;

  return (
    <form className="formulario" onSubmit={enviar}>
      <h2 className="formulario__titulo">{t('Datos del evento', 'Event details')}</h2>
      {perfil && (
        <Mensaje>
          {t(
            'Está enviando la solicitud con su cuenta; podrá darle seguimiento desde “Mi cuenta”.',
            'You are submitting this request with your account; you can track it in “My account”.',
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

      <div className="campo">
        <label htmlFor="nombre">{t('Nombre completo', 'Full name')}</label>
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
          {t('Escriba su nombre completo.', 'Please enter your full name.')}
        </p>
      </div>

      <div className="fila">
        <div className="campo">
          <label htmlFor="correo">{t('Correo electrónico', 'Email address')}</label>
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
            {t(
              'Escriba un correo electrónico válido, por ejemplo nombre@correo.com.',
              'Enter a valid email address, for example name@domain.com.',
            )}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="telefono">{t('Teléfono (opcional)', 'Phone number (optional)')}</label>
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
          <label htmlFor="invitados">{t('Cantidad estimada de invitados (opcional)', 'Estimated guests (optional)')}</label>
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

      <div className="campo">
        <label htmlFor="comentarios">{t('Comentarios o requerimientos especiales (opcional)', 'Comments or special requirements (optional)')}</label>
        <textarea
          id="comentarios"
          name="comentarios"
          rows={4}
          maxLength={2000}
          autoComplete="off"
          placeholder={t(
            'Géneros musicales preferidos, horario de montaje, condiciones del salón o detalles importantes…',
            'Preferred music genres, setup schedule, venue conditions, or important details…',
          )}
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
        {enviando ? t('Enviando…', 'Submitting…') : t('Enviar solicitud', 'Submit quote request')}
      </button>
    </form>
  );
}

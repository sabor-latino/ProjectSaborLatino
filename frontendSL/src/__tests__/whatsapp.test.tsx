import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { SolicitarCotizacion } from '../paginas/SolicitarCotizacion';
import { Solicitudes } from '../paginas/panel/Solicitudes';
import { PreferenciasProvider } from '../preferencias/PreferenciasContext';
import { api } from '../api/cliente';
import { crearMensajeWhatsApp, crearUrlWhatsApp, navegacionWhatsApp } from '../utilidades/whatsapp';
import type { Solicitud } from '../api/tipos';
import { guardarSolicitudPendiente, leerSolicitudPendiente } from '../utilidades/solicitudPendiente';

const { recargar, sesion } = vi.hoisted(() => ({ recargar: vi.fn(), sesion: { admin: false } }));
const solicitud: Solicitud = {
  id: 125, nombreContacto: 'Ana Pérez', correoContacto: null, telefonoContacto: '+506 8888 1111',
  paqueteId: null, paqueteNombre: null, tipoEvento: 'Boda & fiesta', fechaEvento: '2030-04-20T18:00:00',
  lugar: 'Salón #1', estado: 'Pendiente', fechaCreacion: '2026-11-02T12:00:00Z',
  comentarios: 'Salsa\nMerengue', recibidoWhatsAppEn: null, recibidoWhatsAppPorUsuarioId: null,
};
vi.mock('../auth/contexto', () => ({ useAuth: () => ({
  perfil: null, cargando: false, tieneRol: () => sesion.admin,
}) }));
vi.mock('../api/cliente', async (original) => ({
  ...await original<typeof import('../api/cliente')>(),
  api: { post: vi.fn(), patch: vi.fn() }, obtenerToken: () => null,
}));
vi.mock('../hooks/useDatos', () => ({ useDatos: (url: string) => ({
  datos: url.startsWith('/api/solicitudes') ? [solicitud] : [], cargando: false, error: null, recargar,
}) }));

function mostrarFormulario() {
  const vista = render(<MemoryRouter><PreferenciasProvider><SolicitarCotizacion /></PreferenciasProvider></MemoryRouter>);
  for (const [label, value] of [
    ['Nombre completo', 'Ana Pérez'], ['Teléfono de contacto', '+50688881111'],
    ['Tipo de evento', 'Boda'], ['Fecha y hora', '2030-04-20T18:00'], ['Lugar del evento', 'Salón'],
  ]) fireEvent.change(screen.getByLabelText(label), { target: { value } });
  return vista;
}

beforeEach(() => { localStorage.clear(); sessionStorage.clear(); sesion.admin = false; vi.clearAllMocks(); });
afterEach(() => { cleanup(); vi.restoreAllMocks(); });

it('codifica el mensaje y mantiene referencia y datos especiales sin inventar campos opcionales', () => {
  const mensaje = crearMensajeWhatsApp(solicitud, 'es');
  const url = new URL(crearUrlWhatsApp(mensaje));
  expect(url.hostname).toBe('wa.me');
  expect(url.pathname).toBe('/50685262096');
  expect(url.searchParams.get('text')).toBe(mensaje);
  expect(mensaje).toContain('#125');
  expect(mensaje).toContain('Boda & fiesta');
  expect(mensaje).toContain('Salón #1');
  expect(mensaje).toContain('Salsa\nMerengue');
  expect(mensaje).not.toMatch(/undefined|null|Correo|Invitados:/);
  expect(crearMensajeWhatsApp(solicitud, 'en')).toContain('Request #125');
});

it('organiza el formulario con grupos accesibles y explica guardar antes de abrir el chat', () => {
  mostrarFormulario();
  expect(screen.getByRole('group', { name: 'Contacto' })).toContainElement(screen.getByLabelText('Teléfono de contacto'));
  expect(screen.getByRole('group', { name: 'El evento' })).toContainElement(screen.getByLabelText('Fecha y hora'));
  expect(screen.getByRole('group', { name: 'Detalles adicionales' })).toContainElement(screen.getByLabelText('Comentarios (opcional)'));
  expect(screen.getByText('Guardamos su solicitud antes de abrir WhatsApp. Allí podrá revisar el mensaje y pulsar Enviar.')).toBeInTheDocument();
});

it('guarda sin correo, abre WhatsApp después de guardar y reabrir no repite el POST', async () => {
  const abrir = vi.spyOn(navegacionWhatsApp, 'abrir').mockImplementation(() => {});
  let guardar: (value: Solicitud) => void = () => {};
  vi.mocked(api.post).mockReturnValue(new Promise<Solicitud>((resolve) => { guardar = resolve; }));
  mostrarFormulario();
  expect(screen.getByLabelText('Correo electrónico (opcional)')).not.toBeRequired();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  expect(abrir).not.toHaveBeenCalled();
  expect(api.post).toHaveBeenCalledWith('/api/solicitudes', expect.objectContaining({
    telefonoContacto: '+50688881111', correoContacto: undefined, claveIdempotencia: expect.any(String),
  }));
  guardar(solicitud);
  expect(await screen.findByRole('heading', { name: 'Solicitud guardada' })).toBeInTheDocument();
  await waitFor(() => expect(abrir).toHaveBeenCalledTimes(1));
  expect(screen.getByText(/Complete el envío en WhatsApp/)).toBeInTheDocument();
  const enlace = screen.getByRole('link', { name: 'Reabrir WhatsApp' });
  expect(new URL(enlace.getAttribute('href')!).searchParams.get('text')).toContain('#125');
  enlace.addEventListener('click', (event) => event.preventDefault(), { once: true });
  fireEvent.click(enlace);
  expect(api.post).toHaveBeenCalledTimes(1);
});

it('mantiene un solo POST durante el guardado aunque se envíe el formulario dos veces', async () => {
  vi.mocked(api.post).mockReturnValue(new Promise(() => {}));
  mostrarFormulario();
  const form = screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }).closest('form')!;
  fireEvent.submit(form);
  fireEvent.submit(form);
  expect(api.post).toHaveBeenCalledTimes(1);
  expect(screen.getByRole('button', { name: 'Guardando…' })).toBeDisabled();
});

it('no presenta la acción manual a perfiles sin rol administrativo', () => {
  render(<MemoryRouter><PreferenciasProvider><Solicitudes /></PreferenciasProvider></MemoryRouter>);
  fireEvent.click(screen.getByRole('button', { name: /#125/ }));
  expect(screen.queryByRole('button', { name: 'Marcar recibido por WhatsApp' })).not.toBeInTheDocument();
});

it('restaura el éxito al volver o recargar sin nuevo POST ni redirección y permite reiniciar', async () => {
  const abrir = vi.spyOn(navegacionWhatsApp, 'abrir').mockImplementation(() => {});
  vi.mocked(api.post).mockResolvedValue(solicitud);
  const vista = mostrarFormulario();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  await screen.findByRole('heading', { name: 'Solicitud guardada' });
  await waitFor(() => expect(abrir).toHaveBeenCalledTimes(1));
  vista.unmount();
  render(<MemoryRouter><PreferenciasProvider><SolicitarCotizacion /></PreferenciasProvider></MemoryRouter>);
  expect(screen.getByRole('heading', { name: 'Solicitud guardada' })).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Reabrir WhatsApp' })).toHaveAttribute('href', expect.stringContaining('wa.me/50685262096'));
  expect(api.post).toHaveBeenCalledTimes(1);
  expect(abrir).toHaveBeenCalledTimes(1);
  fireEvent.click(screen.getByRole('button', { name: 'Nueva solicitud' }));
  expect(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' })).toBeInTheDocument();
  expect(screen.getByLabelText('Nombre completo')).toHaveFocus();
  expect(sessionStorage.getItem('sabor_solicitud_whatsapp')).toBeNull();
});

it('recupera el intento después de un fallo incierto y reutiliza su clave tras recargar', async () => {
  vi.mocked(api.post).mockRejectedValue(new Error('timeout'));
  const vista = mostrarFormulario();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  await screen.findByText('No se pudo guardar la solicitud. Intente de nuevo.');
  const clave = (vi.mocked(api.post).mock.calls[0][1] as { claveIdempotencia: string }).claveIdempotencia;
  vista.unmount();
  render(<MemoryRouter><PreferenciasProvider><SolicitarCotizacion /></PreferenciasProvider></MemoryRouter>);
  expect(screen.getByLabelText('Lugar del evento')).toHaveValue('Salón');
  fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  await waitFor(() => expect(api.post).toHaveBeenCalledTimes(2));
  expect((vi.mocked(api.post).mock.calls[1][1] as { claveIdempotencia: string }).claveIdempotencia).toBe(clave);
});

it('recupera el botón tras fallo al generar la clave sin crear una solicitud', async () => {
  vi.spyOn(crypto, 'randomUUID').mockImplementationOnce(() => { throw new Error('no disponible'); });
  mostrarFormulario();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  await screen.findByText('No se pudo guardar la solicitud. Intente de nuevo.');
  expect(api.post).not.toHaveBeenCalled();
  expect(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' })).toBeEnabled();
});

it('descarta recuperación de otra cuenta y datos con más de una hora', () => {
  const reloj = vi.spyOn(Date, 'now').mockReturnValue(1800000000000);
  expect(guardarSolicitudPendiente('cuenta-a', { solicitud })).toBe(true);
  expect(leerSolicitudPendiente('cuenta-b')).toBeNull();
  expect(sessionStorage.getItem('sabor_solicitud_whatsapp')).toBeNull();
  guardarSolicitudPendiente('cuenta-a', { solicitud });
  reloj.mockReturnValue(1800000000000 + 60 * 60 * 1000 + 1);
  expect(leerSolicitudPendiente('cuenta-a')).toBeNull();
});

it('rechaza recuperación malformada y solo guarda campos del resumen, nunca cotizaciones del panel', () => {
  sessionStorage.setItem('sabor_solicitud_whatsapp', JSON.stringify({
    actor: 'visitante', creado: Date.now(), intento: { clave: '123', huella: 'x', cuerpo: { nombreContacto: 'Ana' } },
  }));
  expect(leerSolicitudPendiente('visitante')).toBeNull();
  guardarSolicitudPendiente('visitante', { solicitud: { ...solicitud, cotizaciones: [{
    id: 999, solicitudId: 125, monto: 1000, descuento: 0, total: 1000, detalle: null,
    vigenteHasta: '2030-04-20T18:00:00', estado: 'Enviada', fechaCreacion: '2026-11-02T12:00:00Z',
  }] } });
  const guardado = JSON.parse(sessionStorage.getItem('sabor_solicitud_whatsapp')!);
  expect(guardado.solicitud).not.toHaveProperty('cotizaciones');
  expect(guardado.solicitud).not.toHaveProperty('clienteUsuarioId');
});

it('si sessionStorage falla conserva confirmación y permite abrir manualmente, sin redirección automática', async () => {
  const abrir = vi.spyOn(navegacionWhatsApp, 'abrir').mockImplementation(() => {});
  vi.mocked(api.post).mockResolvedValue(solicitud);
  mostrarFormulario();
  vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('bloqueado'); });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  expect(await screen.findByRole('heading', { name: 'Solicitud guardada' })).toBeInTheDocument();
  expect(screen.getByText(/El navegador no permite recuperar/)).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Reabrir WhatsApp' })).toBeInTheDocument();
  expect(abrir).not.toHaveBeenCalled();
  expect(api.post).toHaveBeenCalledTimes(1);
});

it('requiere teléfono, valida correo solo si se escribe y permite copiar resumen sin volver a guardar', async () => {
  vi.spyOn(navegacionWhatsApp, 'abrir').mockImplementation(() => {});
  const copiar = vi.fn().mockResolvedValue(undefined);
  Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: copiar } });
  vi.mocked(api.post).mockResolvedValue(solicitud);
  mostrarFormulario();
  const telefono = screen.getByLabelText('Teléfono de contacto');
  expect(telefono).toBeRequired();
  fireEvent.change(telefono, { target: { value: 'abc' } });
  expect(telefono).toBeInvalid();
  fireEvent.change(telefono, { target: { value: '+50688881111' } });
  fireEvent.blur(telefono);
  expect(telefono).toBeValid();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  await screen.findByRole('heading', { name: 'Solicitud guardada' });
  fireEvent.click(screen.getByRole('button', { name: 'Copiar resumen' }));
  expect(await screen.findByText('Resumen copiado.')).toBeInTheDocument();
  expect(copiar).toHaveBeenCalledWith(expect.stringContaining('#125'));
  expect(api.post).toHaveBeenCalledTimes(1);
});

it('no abre si falla guardar; reintenta con la misma clave y la cambia cuando cambian datos', async () => {
  const abrir = vi.spyOn(navegacionWhatsApp, 'abrir').mockImplementation(() => {});
  vi.mocked(api.post).mockRejectedValue(new Error('sin conexión'));
  mostrarFormulario();
  const enviar = () => fireEvent.click(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' }));
  enviar();
  await screen.findByText('No se pudo guardar la solicitud. Intente de nuevo.');
  expect(screen.getByRole('region', { name: 'No pudimos guardar la solicitud' })).toHaveFocus();
  enviar();
  await waitFor(() => expect(api.post).toHaveBeenCalledTimes(2));
  await waitFor(() => expect(screen.getByRole('button', { name: 'Guardar y abrir WhatsApp' })).toBeEnabled());
  const cuerpo1 = vi.mocked(api.post).mock.calls[0][1] as { claveIdempotencia: string };
  const cuerpo2 = vi.mocked(api.post).mock.calls[1][1] as { claveIdempotencia: string };
  expect(cuerpo1.claveIdempotencia).toBe(cuerpo2.claveIdempotencia);
  fireEvent.change(screen.getByLabelText('Lugar del evento'), { target: { value: 'Otro salón' } });
  enviar();
  await waitFor(() => expect(api.post).toHaveBeenCalledTimes(3));
  expect((vi.mocked(api.post).mock.calls[2][1] as { claveIdempotencia: string }).claveIdempotencia).not.toBe(cuerpo1.claveIdempotencia);
  expect(abrir).not.toHaveBeenCalled();
});

it('marca recepción manual desde panel sin cambiar el estado comercial ni mostrar mailto vacío', async () => {
  sesion.admin = true;
  vi.spyOn(window, 'confirm').mockReturnValue(true);
  vi.mocked(api.post).mockResolvedValue({ ...solicitud, recibidoWhatsAppEn: '2026-11-02T14:00:00Z' });
  render(<MemoryRouter><PreferenciasProvider><Solicitudes /></PreferenciasProvider></MemoryRouter>);
  fireEvent.click(screen.getByRole('button', { name: /#125/ }));
  expect(document.querySelector('a[href="mailto:null"]')).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Marcar recibido por WhatsApp' }));
  await waitFor(() => expect(api.post).toHaveBeenCalledWith('/api/solicitudes/125/whatsapp-recibido'));
  expect(api.patch).not.toHaveBeenCalled();
  expect(recargar).toHaveBeenCalled();
  expect(document.activeElement).toHaveTextContent('Recepción de WhatsApp confirmada manualmente.');
});

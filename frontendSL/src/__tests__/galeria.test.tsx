import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import App from '../App';
import { PreferenciasProvider } from '../preferencias/PreferenciasContext';

vi.mock('../auth/contexto', () => ({
  useAuth: () => ({
    perfil: { nombreCompleto: 'Admin Demo', roles: ['Superadministrador'] },
    cargando: false, tieneRol: () => true, cerrarSesion: vi.fn(),
  }),
}));
const fotos = [
  { id: 1, titulo: 'Boda tropical', tituloEn: 'Tropical wedding', imagenUrl: 'https://example.com/boda.jpg', orden: 0, activo: true },
  { id: 2, titulo: 'Marimba en vivo', tituloEn: null, imagenUrl: 'https://example.com/marimba.jpg', orden: 1, activo: true },
];
const dialogShowModal = Object.getOwnPropertyDescriptor(HTMLDialogElement.prototype, 'showModal');
const dialogClose = Object.getOwnPropertyDescriptor(HTMLDialogElement.prototype, 'close');
beforeEach(() => {
  Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
    configurable: true, value: function (this: HTMLDialogElement) { this.open = true; },
  });
  Object.defineProperty(HTMLDialogElement.prototype, 'close', {
    configurable: true, value: function (this: HTMLDialogElement) {
      this.open = false;
      this.dispatchEvent(new Event('close'));
    },
  });
  localStorage.clear();
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (_input, init) =>
    new Response(init?.method === 'PUT' ? null : JSON.stringify(fotos), {
      status: init?.method === 'PUT' ? 204 : 200,
      headers: { 'Content-Type': 'application/json' },
    }));
});
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  for (const [name, descriptor] of [['showModal', dialogShowModal], ['close', dialogClose]] as const) {
    if (descriptor) Object.defineProperty(HTMLDialogElement.prototype, name, descriptor);
    else Reflect.deleteProperty(HTMLDialogElement.prototype, name);
  }
});

it('abre galería desde su ruta y menú propios y guarda el título inglés al editar', async () => {
  render(<MemoryRouter initialEntries={['/panel/galeria']}><PreferenciasProvider><App modoPortal="admin" /></PreferenciasProvider></MemoryRouter>);
  expect(await screen.findByRole('link', { name: 'Galería' })).toHaveAttribute('href', '/panel/galeria');
  fireEvent.click((await screen.findAllByRole('button', { name: 'Editar' }))[0]);
  const ingles = screen.getByLabelText('Título (Inglés)');
  expect(ingles).toHaveValue('Tropical wedding');
  fireEvent.change(ingles, { target: { value: 'Wedding celebration' } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));
  await waitFor(() => expect(fetch).toHaveBeenCalledWith(
    expect.stringContaining('/api/galeria/1'),
    expect.objectContaining({ method: 'PUT', body: expect.stringContaining('"tituloEn":"Wedding celebration"') }),
  ));
  expect(await screen.findByText('Se guardaron los cambios.')).toBeInTheDocument();
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
});
it('traduce título y texto alternativo públicos y conserva español si no hay traducción', async () => {
  render(<MemoryRouter initialEntries={['/galeria']}><PreferenciasProvider><App modoPortal="publico" /></PreferenciasProvider></MemoryRouter>);
  expect(await screen.findByRole('img', { name: 'Boda tropical' })).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: /\(EN\)/i }));
  expect(await screen.findByRole('img', { name: 'Tropical wedding' })).toBeInTheDocument();
  expect(screen.getByText('Tropical wedding')).toBeInTheDocument();
  expect(screen.getByRole('img', { name: 'Marimba en vivo' })).toBeInTheDocument();
});

it('traduce fotos de referencia cuando galería está vacía', async () => {
  vi.mocked(fetch).mockResolvedValue(new Response('[]', {
    status: 200, headers: { 'Content-Type': 'application/json' },
  }));
  render(<MemoryRouter initialEntries={['/galeria']}><PreferenciasProvider><App modoPortal="publico" /></PreferenciasProvider></MemoryRouter>);
  await screen.findByRole('img', { name: 'Bodas' });
  fireEvent.click(screen.getByRole('button', { name: /\(EN\)/i }));
  expect(await screen.findByRole('img', { name: 'Weddings' })).toBeInTheDocument();
  expect(screen.getByText('Weddings')).toBeInTheDocument();
});

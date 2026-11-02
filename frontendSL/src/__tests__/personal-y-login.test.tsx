import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '../auth/AuthContext';
import { Catalogo } from '../paginas/panel/Catalogo';
import { Login } from '../paginas/Login';
import { PreferenciasProvider } from '../preferencias/PreferenciasContext';

const dialogShowModal = Object.getOwnPropertyDescriptor(HTMLDialogElement.prototype, 'showModal');
const dialogClose = Object.getOwnPropertyDescriptor(HTMLDialogElement.prototype, 'close');

describe('QA Frontend — Acceso Dual en Login y Sección de Personal con Modales', () => {
  beforeEach(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
      configurable: true,
      value: function (this: HTMLDialogElement) {
        this.open = true;
      },
    });
    Object.defineProperty(HTMLDialogElement.prototype, 'close', {
      configurable: true,
      value: function (this: HTMLDialogElement) {
        this.open = false;
        this.dispatchEvent(new Event('close'));
      },
    });

    localStorage.clear();

    vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
      const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;

      if (url.includes('/api/empleados')) {
        return new Response(
          JSON.stringify([
            {
              id: 1,
              nombreCompleto: 'Carlos Técnico',
              cedula: '1-1111-1111',
              puesto: 'Ingeniero de Sonido',
              telefono: '8888-9999',
              usuarioId: 10,
              activo: true,
            },
          ]),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }

      if (url.includes('/api/usuarios?rol=Trabajador')) {
        return new Response(
          JSON.stringify([
            {
              id: 10,
              nombreCompleto: 'Carlos Técnico',
              email: 'carlos@saborlatino.cr',
              rol: 'Trabajador',
              activo: true,
              fechaRegistro: '2026-01-01T00:00:00Z',
            },
          ]),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }

      if (url.includes('/api/servicios')) {
        return new Response(JSON.stringify([]), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        });
      }

      return new Response(JSON.stringify([]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    });
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    for (const [name, descriptor] of [
      ['showModal', dialogShowModal],
      ['close', dialogClose],
    ] as const) {
      if (descriptor) Object.defineProperty(HTMLDialogElement.prototype, name, descriptor);
      else Reflect.deleteProperty(HTMLDialogElement.prototype, name);
    }
  });

  it('permite alternar entre pestañas de Acceso Administrativo y Acceso Empleado en el Login', () => {
    render(
      <MemoryRouter initialEntries={['/login']}>
        <PreferenciasProvider>
          <AuthProvider>
            <Login />
          </AuthProvider>
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    const pestanaAdmin = screen.getByRole('tab', { name: /Acceso Administrativo/i });
    const pestanaEmpleado = screen.getByRole('tab', { name: /Acceso Empleado \/ Personal/i });

    expect(pestanaAdmin).toHaveAttribute('aria-selected', 'true');
    expect(pestanaEmpleado).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('heading', { name: /Portal Administrativo/i })).toBeInTheDocument();

    fireEvent.click(pestanaEmpleado);

    expect(pestanaAdmin).toHaveAttribute('aria-selected', 'false');
    expect(pestanaEmpleado).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('heading', { name: /Portal de Personal y Empleados/i })).toBeInTheDocument();
    expect(
      screen.getByText(/Portal interno para el personal técnico y operativo de Sabor Latino/i),
    ).toBeInTheDocument();
  });

  it('renderiza la pestaña de Personal en el Catálogo y permite abrir el modal de nuevo colaborador', async () => {
    render(
      <MemoryRouter>
        <PreferenciasProvider>
          <AuthProvider>
            <Catalogo />
          </AuthProvider>
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    const botonPersonal = screen.getByRole('button', { name: 'Personal' });
    fireEvent.click(botonPersonal);

    expect(await screen.findByRole('heading', { name: 'Personal' })).toBeInTheDocument();
    expect(await screen.findByText('Carlos Técnico')).toBeInTheDocument();
    expect(screen.getByText('1-1111-1111')).toBeInTheDocument();
    expect(screen.getByText('Ingeniero de Sonido')).toBeInTheDocument();
    expect(screen.getByText('carlos@saborlatino.cr')).toBeInTheDocument();

    const botonNuevo = screen.getByRole('button', { name: '+ Nuevo' });
    fireEvent.click(botonNuevo);

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByLabelText(/Nombre completo/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Cédula/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Puesto/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Cuenta de trabajador/i)).toBeInTheDocument();

    // Cerrar modal
    const botonCerrar = screen.getByRole('button', { name: 'Cerrar' });
    fireEvent.click(botonCerrar);

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});

import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../App';
import type { Paquete } from '../api/tipos';
import { AuthProvider } from '../auth/AuthContext';
import { TarjetaPaquete, obtenerInsigniaComercial } from '../componentes/TarjetaPaquete';
import { Catalogo } from '../paginas/panel/Catalogo';
import { InicioAdmin } from '../paginas/panel/InicioAdmin';
import { PromocionesAdmin } from '../paginas/panel/PromocionesAdmin';
import { PreferenciasProvider } from '../preferencias/PreferenciasContext';

const PAQUETE_TECNICO: Paquete = {
  id: 10,
  servicioId: 1,
  servicioNombre: 'Discomóvil e Iluminación',
  servicioNombreEn: 'Mobile DJ & Lighting',
  nombre: 'Paquete Gala Imperial',
  nombreEn: 'Imperial Gala Package',
  descripcion: 'Sonido profesional de alta fidelidad e iluminación robótica.',
  descripcionEn: 'High-fidelity professional sound and intelligent lighting.',
  precioBase: 250000,
  imagenUrl: null,
  potenciaSonido: '4 000 W RMS con subwoofers de 18"',
  coberturaAforo: 'Hasta 200 invitados',
  duracionMontaje: '90 minutos antes del evento',
  requerimientosElectricos: '2 circuitos independientes de 120 V / 20 A',
  espacioMinimo: '4 m × 2,5 m techado',
  fichaTecnica: 'Incluye consola digital de 16 canales y técnico de sonido dedicado.',
  fichaTecnicaEn: 'Includes 16-channel digital mixer and dedicated audio engineer.',
  activo: true,
  equipos: [
    {
      equipoId: 1,
      equipoNombre: 'Subwoofer activo 18"',
      equipoNombreEn: '18" Active Subwoofer',
      especificacionTecnica: '1200 W RMS',
      cantidad: 2,
    },
  ],
};

describe('Sabor Latino — Accesibilidad WCAG AAA, Idioma ES/EN, Paquetes Técnicos y Separación de Portales', () => {
  beforeEach(() => {
    window.localStorage.clear();
    document.documentElement.lang = 'es';
    delete document.documentElement.dataset.fontScale;
    delete document.documentElement.dataset.contrast;

    vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
      const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;

      if (url.includes('/api/contenido-inicio')) {
        return new Response(
          JSON.stringify({
            etiquetaPortada: 'Bodas · Quince años · Eventos corporativos',
            etiquetaPortadaEn: 'Weddings · Sweet 15s · Corporate events',
            tituloPortada: 'Que su fiesta suene en vivo',
            tituloPortadaEn: 'Bring your celebration to life with live sound',
            subtituloPortada: 'Karaoke, marimba, discomóvil y orquesta en toda Costa Rica.',
            subtituloPortadaEn: 'Karaoke, marimba, mobile DJ, and orchestra across Costa Rica.',
            fotoPortadaUrl: null,
            fotoCotizarUrl: null,
            paso1Titulo: 'Cuéntenos sobre su evento',
            paso1TituloEn: 'Tell us about your event',
            paso1Texto: 'Fecha, lugar y cantidad de invitados.',
            paso1TextoEn: 'Date, venue, and guest count.',
            paso2Titulo: 'Reciba su cotización formal',
            paso2TituloEn: 'Receive your formal quote',
            paso2Texto: 'Propuesta clara en colones.',
            paso2TextoEn: 'Clear proposal in colones.',
            paso3Titulo: 'Disfrute la celebración',
            paso3TituloEn: 'Enjoy the celebration',
            paso3Texto: 'Montaje y prueba de sonido incluidos.',
            paso3TextoEn: 'Setup and soundcheck included.',
            ctaTitulo: 'Aparte su fecha antes de que se agote la agenda',
            ctaTituloEn: 'Reserve your date before the calendar fills up',
            ctaSubtitulo: 'Solicitar su cotización no tiene costo.',
            ctaSubtituloEn: 'Requesting your quote is free.',
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }

      if (url.includes('/api/promociones')) {
        return new Response(
          JSON.stringify([
            {
              id: 5,
              nombre: 'Especial de Graduaciones y Fin de Año',
              nombreEn: 'Graduation & Year-End Special',
              descripcion: 'Descuento especial en paquetes con iluminación escénica.',
              descripcionEn: 'Special discount on packages with stage lighting.',
              etiquetaTemporada: 'Temporada de Fin de Año',
              etiquetaTemporadaEn: 'Year-End Season',
              porcentajeDescuento: 20,
              codigoPromocional: 'FIESTA20',
              fechaInicio: '2026-01-01T00:00:00Z',
              fechaFin: '2027-12-31T23:59:59Z',
              imagenUrl: null,
              destacadaEnInicio: true,
              activo: true,
              vigente: true,
              paquetes: [
                {
                  paqueteId: 10,
                  paqueteNombre: 'Paquete Gala Imperial',
                  paqueteNombreEn: 'Imperial Gala Package',
                  precioBase: 250000,
                  precioPromocional: 200000,
                },
              ],
            },
          ]),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }

      if (url.includes('/api/paquetes')) {
        return new Response(JSON.stringify([PAQUETE_TECNICO]), {
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
    vi.restoreAllMocks();
  });

  it('renderiza la portada, promociones de temporada y permite cambiar tamaño de fuente, contraste e idioma ES/EN', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <PreferenciasProvider>
          <AuthProvider>
            <App modoPortal="publico" />
          </AuthProvider>
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByText(/Especial de Graduaciones y Fin de Año/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Sabor Latino/i).length).toBeGreaterThan(0);

    // En modo público no hay enlaces de inicio de sesión ni de creación de cuenta
    expect(screen.queryByText(/Iniciar sesión/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Crear cuenta/i)).not.toBeInTheDocument();

    // Verificar cambio de escala de fuente (A -> A+ -> A++)
    const botonGrande = screen.getByRole('button', { name: /A\+ —/i });
    fireEvent.click(botonGrande);
    expect(document.documentElement.dataset.fontScale).toBe('grande');

    const botonExtra = screen.getByRole('button', { name: /A\+\+ —/i });
    fireEvent.click(botonExtra);
    expect(document.documentElement.dataset.fontScale).toBe('extra');

    // Verificar cambio de contraste WCAG AAA (alto-claro y alto-oscuro)
    const botonAltoClaro = screen.getByRole('button', { name: /Alto contraste claro/i });
    fireEvent.click(botonAltoClaro);
    expect(document.documentElement.dataset.contrast).toBe('alto-claro');

    const botonAltoOscuro = screen.getByRole('button', { name: /Alto contraste oscuro/i });
    fireEvent.click(botonAltoOscuro);
    expect(document.documentElement.dataset.contrast).toBe('alto-oscuro');

    // Verificar cambio de idioma ES -> EN
    const botonIngles = screen.getByRole('button', { name: /\(EN\)/i });
    fireEvent.click(botonIngles);
    expect(document.documentElement.lang).toBe('en');
    expect(await screen.findByText(/Bring your celebration to life with live sound/i)).toBeInTheDocument();
    expect(screen.getByText(/Graduation & Year-End Special/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Imperial Gala Package/i).length).toBeGreaterThan(0);
    expect(await screen.findByRole('img', { name: 'A full dance floor' })).toBeInTheDocument();
  });

  it('oculta las rutas /login y /panel en el portal público (muestra 404) y habilita el Portal Administrativo en modo admin', async () => {
    const publicLogin = render(
      <MemoryRouter initialEntries={['/login']}>
        <PreferenciasProvider>
          <AuthProvider>
            <App modoPortal="publico" />
          </AuthProvider>
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByText('404')).toBeInTheDocument();
    expect(screen.getByText(/Esta página no existe/i)).toBeInTheDocument();
    publicLogin.unmount();

    const publicPanel = render(
      <MemoryRouter initialEntries={['/panel']}>
        <PreferenciasProvider>
          <AuthProvider>
            <App modoPortal="publico" />
          </AuthProvider>
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByText('404')).toBeInTheDocument();
    publicPanel.unmount();

    render(
      <MemoryRouter initialEntries={['/login']}>
        <PreferenciasProvider>
          <AuthProvider>
            <App modoPortal="admin" />
          </AuthProvider>
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { name: /Portal Administrativo/i })).toBeInTheDocument();
    expect(screen.getByText(/Acceso interno restringido/i)).toBeInTheDocument();
    expect(screen.queryByText(/Cree una aquí/i)).not.toBeInTheDocument();
  });

  it('muestra especificaciones técnicas desplegables e insignia de promoción en TarjetaPaquete', () => {
    render(
      <MemoryRouter>
        <PreferenciasProvider>
          <TarjetaPaquete
            paquete={PAQUETE_TECNICO}
            promocion={{
              id: 5,
              etiqueta: 'Temporada de Fin de Año',
              porcentajeDescuento: 20,
              precioPromocional: 200000,
            }}
          />
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(screen.getByText(/Temporada de Fin de Año · -20%/i)).toBeInTheDocument();
    expect(screen.getByText(/Especificaciones técnicas y montaje/i)).toBeInTheDocument();
    expect(screen.getAllByText(/4 000 W RMS con subwoofers de 18"/i)[0]).toBeInTheDocument();
    expect(screen.getByText(/Hasta 200 invitados/i)).toBeInTheDocument();
    expect(screen.getByText(/2 circuitos independientes de 120 V \/ 20 A/i)).toBeInTheDocument();
    expect(screen.getByText(/⭐ Más cotizado/i)).toBeInTheDocument();
    expect(screen.getByText(/4 horas de música incluidas · Hora adicional disponible/i)).toBeInTheDocument();

    const mockT = (es: string) => es;
    expect(obtenerInsigniaComercial({ ...PAQUETE_TECNICO, servicioNombre: 'Sonorización', nombre: 'Sonorización para grupos o DJs' }, mockT)).toBe('⚡ Alta fidelidad');
    expect(obtenerInsigniaComercial({ ...PAQUETE_TECNICO, servicioNombre: 'Música en vivo', nombre: 'Marimba Orquesta Son del Gallo' }, mockT)).toBe('💍 Recomendado para bodas');
    expect(obtenerInsigniaComercial({ ...PAQUETE_TECNICO, servicioNombre: 'Karaoke', nombre: 'DJ Open Format' }, mockT)).toBe('⭐ Más cotizado');
  });

  it('renderiza las 3 secciones administrativas: InicioAdmin, PromocionesAdmin y Catalogo (Paquetes)', async () => {
    const { unmount } = render(
      <MemoryRouter>
        <PreferenciasProvider>
          <InicioAdmin />
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByDisplayValue(/Que su fiesta suene en vivo/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Título de portada \(Inglés\)/i)).toBeInTheDocument();
    unmount();

    const promoRender = render(
      <MemoryRouter>
        <PreferenciasProvider>
          <PromocionesAdmin />
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByText(/Especial de Graduaciones y Fin de Año/i)).toBeInTheDocument();
    expect(screen.getByText(/FIESTA20/i)).toBeInTheDocument();
    promoRender.unmount();

    render(
      <MemoryRouter>
        <PreferenciasProvider>
          <Catalogo />
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    await screen.findByText(/Todavía no hay registros/i);
    fireEvent.click(screen.getByRole('button', { name: 'Paquetes' }));
    expect(await screen.findByText(/Paquete Gala Imperial/i)).toBeInTheDocument();
    expect(screen.getByText(/4 000 W RMS/i)).toBeInTheDocument();
  });

  it('permite expandir y retraer las barras de accesibilidad e idioma de forma independiente', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <PreferenciasProvider>
          <AuthProvider>
            <App modoPortal="publico" />
          </AuthProvider>
        </PreferenciasProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByText(/Especial de Graduaciones y Fin de Año/i)).toBeInTheDocument();

    const triggerIdioma = screen.getByRole('button', { name: /Opciones de idioma/i });
    const triggerFuente = screen.getByRole('button', { name: /Opciones de tamaño de fuente/i });
    const triggerContraste = screen.getByRole('button', { name: /Opciones de contraste/i });

    // Inicialmente están retraídos (aria-expanded === false)
    expect(triggerIdioma).toHaveAttribute('aria-expanded', 'false');
    expect(triggerFuente).toHaveAttribute('aria-expanded', 'false');
    expect(triggerContraste).toHaveAttribute('aria-expanded', 'false');

    // Expandir idioma independientemente
    fireEvent.click(triggerIdioma);
    expect(triggerIdioma).toHaveAttribute('aria-expanded', 'true');
    expect(triggerFuente).toHaveAttribute('aria-expanded', 'false');
    expect(triggerContraste).toHaveAttribute('aria-expanded', 'false');

    // Expandir tamaño de fuente de forma independiente
    fireEvent.click(triggerFuente);
    expect(triggerIdioma).toHaveAttribute('aria-expanded', 'true');
    expect(triggerFuente).toHaveAttribute('aria-expanded', 'true');
    expect(triggerContraste).toHaveAttribute('aria-expanded', 'false');

    // Retraer idioma
    fireEvent.click(triggerIdioma);
    expect(triggerIdioma).toHaveAttribute('aria-expanded', 'false');
    expect(triggerFuente).toHaveAttribute('aria-expanded', 'true');

    // Expandir y retraer contraste
    fireEvent.click(triggerContraste);
    expect(triggerContraste).toHaveAttribute('aria-expanded', 'true');
    fireEvent.click(triggerContraste);
    expect(triggerContraste).toHaveAttribute('aria-expanded', 'false');
  });
});

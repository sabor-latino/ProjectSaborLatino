import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { CampoImagen } from '../componentes/panel/CampoImagen';
import * as imagenUtils from '../utilidades/imagen';
import { api } from '../api/cliente';

describe('CampoImagen — Subida de Fotos desde el Celular', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('muestra el botón de subida desde el celular cuando no hay foto seleccionada', () => {
    const alCambiar = vi.fn();
    render(
      <CampoImagen
        id="test-img"
        nombre="imagenUrl"
        valor=""
        alCambiar={alCambiar}
        ayuda="Ayuda de prueba"
      />
    );

    expect(screen.getByRole('button', { name: /Tomar foto o elegir del celular/i })).toBeInTheDocument();
    expect(screen.getByText('Ayuda de prueba')).toBeInTheDocument();

    const inputArchivo = screen.getByLabelText(/Seleccionar o tomar fotografía/i);
    expect(inputArchivo).toHaveAttribute('accept', 'image/jpeg,image/png,image/webp');
  });

  it('permite alternar entre subida desde celular y escribir enlace manual', () => {
    const alCambiar = vi.fn();
    render(
      <CampoImagen
        id="test-img"
        nombre="imagenUrl"
        valor=""
        alCambiar={alCambiar}
      />
    );

    const botonAlternativa = screen.getByRole('button', { name: /O escribir un enlace URL manual/i });
    fireEvent.click(botonAlternativa);

    const inputManual = screen.getByPlaceholderText('https://ejemplo.com/foto.jpg');
    expect(inputManual).toBeInTheDocument();

    fireEvent.change(inputManual, { target: { value: 'https://ejemplo.com/manual.jpg' } });
    expect(alCambiar).toHaveBeenCalledWith('https://ejemplo.com/manual.jpg');
  });

  it('muestra la vista previa, botón de cambiar y botón de quitar cuando hay una imagen', () => {
    const alCambiar = vi.fn();
    render(
      <CampoImagen
        id="test-img"
        nombre="imagenUrl"
        valor="http://localhost:5238/uploads/imagenes/foto1.webp"
        alCambiar={alCambiar}
      />
    );

    const miniatura = screen.getByRole('img', { name: 'Vista previa' });
    expect(miniatura).toHaveAttribute('src', 'http://localhost:5238/uploads/imagenes/foto1.webp');

    const botonCambiar = screen.getByRole('button', { name: /📷 Cambiar foto/i });
    expect(botonCambiar).toBeInTheDocument();

    const botonQuitar = screen.getByRole('button', { name: /Quitar/i });
    fireEvent.click(botonQuitar);
    expect(alCambiar).toHaveBeenCalledWith('');
  });

  it('llama a subirImagen y actualiza el valor al seleccionar un archivo', async () => {
    const alCambiar = vi.fn();
    vi.spyOn(imagenUtils, 'subirImagen').mockResolvedValue({
      url: 'http://localhost:5238/uploads/imagenes/celular-abc.webp',
      rutaRelativa: '/uploads/imagenes/celular-abc.webp',
      nombreOriginal: 'foto_evento.jpg',
      tamanoBytes: 154000,
    });

    render(
      <CampoImagen
        id="test-img"
        nombre="imagenUrl"
        valor=""
        alCambiar={alCambiar}
      />
    );

    const inputOculto = screen.getByLabelText(/Seleccionar o tomar fotografía/i);
    const archivoSimulado = new File(['dummy-bytes'], 'foto_evento.jpg', { type: 'image/jpeg' });

    fireEvent.change(inputOculto, { target: { files: [archivoSimulado] } });

    await waitFor(() => {
      expect(imagenUtils.subirImagen).toHaveBeenCalledWith(archivoSimulado);
      expect(alCambiar).toHaveBeenCalledWith('http://localhost:5238/uploads/imagenes/celular-abc.webp');
    });
  });

  it('muestra el mensaje de error cuando subirImagen rechaza la promesa y notifica enSubiendo', async () => {
    const alCambiar = vi.fn();
    const enSubiendo = vi.fn();
    vi.spyOn(imagenUtils, 'subirImagen').mockRejectedValue(new Error('Safari no pudo leer la imagen'));

    render(
      <CampoImagen
        id="test-img-error"
        nombre="imagenUrl"
        valor=""
        alCambiar={alCambiar}
        enSubiendo={enSubiendo}
      />
    );

    const inputOculto = screen.getByLabelText(/Seleccionar o tomar fotografía/i);
    const archivoSimulado = new File(['dummy-bytes'], 'corrupta.heic', { type: 'image/heic' });

    fireEvent.change(inputOculto, { target: { files: [archivoSimulado] } });

    expect(await screen.findByText(/Safari no pudo leer la imagen/i)).toBeInTheDocument();
    expect(alCambiar).not.toHaveBeenCalled();
    expect(enSubiendo).toHaveBeenCalledWith(true);
    expect(enSubiendo).toHaveBeenCalledWith(false);
  });

  it('subirImagen recurre al archivo original cuando la compresión de canvas falla en Safari', async () => {
    const postFormSpy = vi.spyOn(api, 'postForm').mockResolvedValue({
      url: 'http://localhost:5238/uploads/imagenes/fallback.jpg',
      rutaRelativa: '/uploads/imagenes/fallback.jpg',
      nombreOriginal: 'foto_safari.jpg',
      tamanoBytes: 50000,
    });

    const originalImage = globalThis.Image;
    // Simular que en Safari Image falla al decodificar
    globalThis.Image = class {
      set onload(_fn: () => void) {}
      set onerror(fn: () => void) {
        setTimeout(() => fn(), 5);
      }
      set src(_val: string) {}
    } as unknown as typeof Image;

    try {
      const archivoSafari = new File(['mock-bytes'], 'foto_safari.jpg', { type: 'image/jpeg' });
      const resultado = await imagenUtils.subirImagen(archivoSafari);

      expect(resultado.url).toBe('http://localhost:5238/uploads/imagenes/fallback.jpg');
      expect(postFormSpy).toHaveBeenCalledWith(
        '/api/archivos/subir-imagen',
        expect.any(FormData)
      );
    } finally {
      globalThis.Image = originalImage;
    }
  });
});

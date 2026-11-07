import { test, expect } from '@playwright/test';

test.describe('Pruebas en Motor WebKit / Safari (Desktop y Mobile iPhone)', () => {
  test('carga la portada pública en WebKit sin errores de consola', async ({ page }) => {
    const erroresConsola: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        erroresConsola.push(msg.text());
      }
    });

    await page.goto('http://localhost:54526/');
    await expect(page).toHaveTitle(/Sabor Latino/i);

    // Verificar que los encabezados principales se rendericen en WebKit
    const encabezado = page.locator('h1');
    await expect(encabezado.first()).toBeVisible();

    // Filtrar advertencias locales de desarrollo si las hubiera
    const erroresReales = erroresConsola.filter(
      (e) => !e.includes('SSL') && !e.includes('favicon'),
    );
    expect(erroresReales).toEqual([]);
  });

  test('carga la galería de eventos en WebKit', async ({ page }) => {
    await page.goto('http://localhost:54526/galeria');
    await expect(page).toHaveTitle(/Galería|Sabor Latino/i);

    // Esperar a que la página renderice el contenido en WebKit
    const contenedor = page.locator('.galeria, main, section');
    await expect(contenedor.first()).toBeVisible();
  });

  test('carga el portal administrativo en WebKit en puerto independiente 54527', async ({ page }) => {
    await page.goto('http://localhost:54527/login');
    await expect(page).toHaveTitle(/Sabor Latino/i);

    // Verificar que el formulario de login y el botón de entrar estén visibles en WebKit
    const botonEntrar = page.locator('button[type="submit"]');
    await expect(botonEntrar).toBeVisible();
  });
});

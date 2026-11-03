import { mkdir, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { ChromeMcp, textContent } from './mcp-client.mjs';

// Comparación pública de solo lectura, solicitada por el usuario. Sin formularios,
// cookies de sesiones personales, mensajes ni navegación por enlaces de terceros.
const references = [
  'https://eventmusiccostarica.com/',
  'https://www.livemusiccr.com/',
  'https://design-system.service.gov.uk/components/button/',
];
const directory = resolve('tasks/qa-whatsapp/.artifacts');
await mkdir(directory, { recursive: true });
const stamp = Date.now();
const client = new ChromeMcp();
const results = [];
try {
  await client.start();
  for (const [index, url] of references.entries()) {
    try {
      const opened = textContent(await client.call('new_page', { url, isolatedContext: `reference-${index}`, timeout: 30000 }));
      const pageId = Number(opened.match(/^(\d+):.*\[selected\].*$/m)?.[1]);
      if (!Number.isFinite(pageId)) throw new Error('No page ID');
      for (const width of [1440, 390]) {
        await client.call('emulate', { pageId, viewport: `${width}x900x1` });
        const observations = textContent(await client.call('evaluate_script', { pageId, waitForStableDom: true, function: `async () => {
          await Promise.race([document.fonts.ready, new Promise(r => setTimeout(r, 8000))]);
          return {url: location.href, viewport: innerWidth, documentWidth: document.documentElement.scrollWidth,
            headings: [...document.querySelectorAll('h1,h2')].map(el => ({text: el.innerText, size: getComputedStyle(el).fontSize})).slice(0,12),
            controls: [...document.querySelectorAll('button,a')].map(el => { const r = el.getBoundingClientRect(); const s = getComputedStyle(el); return {text: el.innerText.trim(),role: el.tagName, x:r.x,y:r.y,width:r.width,height:r.height,radius:s.borderRadius,fontSize:s.fontSize,color:s.color,background:s.backgroundColor}; }).filter(el => el.text && el.width && el.height && el.y >= 0 && el.y < innerHeight).slice(0,24)};
        }` }));
        const screenshot = resolve(directory, `${stamp}-reference-${index}-${width}.png`);
        await client.call('take_screenshot', { pageId, filePath: screenshot, fullPage: false });
        results.push({ url, width, observations, screenshot });
      }
    } catch (error) { results.push({ url, error: error.message }); }
  }
} finally { await client.close(); }
const report = resolve(directory, `${stamp}-references.json`);
await writeFile(report, JSON.stringify({ time: new Date().toISOString(), results }, null, 2));
console.log(JSON.stringify({ report, results }, null, 2));

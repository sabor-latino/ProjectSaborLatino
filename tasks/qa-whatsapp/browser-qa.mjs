import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ChromeMcp, textContent } from './mcp-client.mjs';

const directory = dirname(fileURLToPath(import.meta.url));
const artifacts = join(directory, '.artifacts');
const mode = process.argv[2] ?? 'schemas';
const localUrl = process.argv[3] ?? process.env.SABOR_QA_PUBLIC_URL ?? 'http://localhost:54526/';
if (!['schemas', 'smoke'].includes(mode)) throw new Error('Modo válido: schemas | smoke [URL-local] [texto-esperado]');
if (mode === 'smoke') {
  const url = new URL(localUrl);
  if (!['http://localhost:54526', 'http://localhost:54527', 'http://localhost:54528', 'http://localhost:54529'].includes(url.origin) || url.username || url.password) {
    throw new Error('QA solo admite los portales locales demo 54526/54527/54528/54529.');
  }
}
const client = new ChromeMcp();
await mkdir(artifacts, { recursive: true });
try {
  const server = await client.start();
  const list = await client.request('tools/list', {});
  await writeFile(join(artifacts, 'tools.json'), JSON.stringify({ server, ...list }, null, 2));
  console.log(JSON.stringify({ server: server.serverInfo, tools: list.tools.map(tool => tool.name), schemas: join(artifacts, 'tools.json') }, null, 2));
  if (mode === 'smoke') {
    const opened = textContent(await client.call('new_page', { url: localUrl, isolatedContext: 'sabor-qa', timeout: 30000 }));
    const selected = opened.match(/^(\d+):.*\[selected\].*$/m);
    if (!selected) throw new Error('No se encontró ID de página en respuesta MCP: ' + opened);
    const pageId = Number(selected[1]);
    await client.call('wait_for', { pageId, text: [process.argv[4] ?? 'Sabor Latino'], timeout: 20000 });
    await client.call('evaluate_script', { pageId, waitForStableDom: false, function: `async () => {
      await Promise.race([Promise.all([document.fonts.ready,...[...document.images].map(img=>img.complete?Promise.resolve():new Promise(resolve=>{img.addEventListener('load',resolve,{once:true});img.addEventListener('error',resolve,{once:true});}))]),new Promise(resolve=>setTimeout(resolve,10000))]);
      return {images:[...document.images].map(img=>({complete:img.complete,loaded:img.naturalWidth>0})),fonts:document.fonts.status};
    }` });
    const snapshot = textContent(await client.call('take_snapshot', { pageId }));
    const consoleMessages = textContent(await client.call('list_console_messages', { pageId, types: ['error', 'warn'] }));
    const network = textContent(await client.call('list_network_requests', { pageId }));
    const dom = textContent(await client.call('evaluate_script', { pageId, waitForStableDom: false,
      function: `() => ({url: location.href, title: document.title, lang: document.documentElement.lang,
        viewport: innerWidth, documentWidth: document.documentElement.scrollWidth,
        headings: [...document.querySelectorAll('h1,h2')].map(el => el.textContent.trim()),
        whatsappLinks: [...document.querySelectorAll('a[href]')].filter(el => el.href.startsWith('https://wa.me/')).map(el => ({label:el.textContent.trim(),href:el.href})),
        loginLinks: [...document.querySelectorAll('a[href]')].filter(el => /\\/(login|registro|panel)(\\/|$)/.test(new URL(el.href).pathname)).map(el => el.textContent.trim())})`,
    }));
    const prefix = `smoke-${new URL(localUrl).port}-${Date.now()}`;
    const screenshot = join(artifacts, prefix + '.png');
    await client.call('take_screenshot', { pageId, filePath: screenshot, fullPage: true });
    const reportPath = join(artifacts, prefix + '.json');
    await writeFile(reportPath, JSON.stringify({ time: new Date().toISOString(), url: localUrl, snapshot, consoleMessages, network, dom, screenshot }, null, 2));
    console.log(JSON.stringify({ reportPath, screenshot, dom, consoleMessages, network }, null, 2));
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
} finally {
  await client.close();
}

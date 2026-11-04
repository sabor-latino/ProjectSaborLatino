import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ChromeMcp, textContent } from '../qa-whatsapp/mcp-client.mjs';

// Only known production documents; never derive navigation from page content.
const publicOrigin = 'https://sabor-latino-seven.vercel.app';
const adminOrigin = 'https://sabor-latino-admin.vercel.app';
const apiOrigin = 'https://saborlatino-api-8dde8f3e.azurewebsites.net';
const pages = [
  ['inicio', `${publicOrigin}/`, false],
  ['paquetes', `${publicOrigin}/paquetes`, true],
  ['galeria', `${publicOrigin}/galeria`, false],
  ['solicitar', `${publicOrigin}/solicitar`, true],
  ['admin-login', `${adminOrigin}/`, true],
];
const allowed = new Set(pages.map(([, url]) => url));
// The known administrative root deliberately redirects to its login route.
const expectedDestinations = new Map([[`${adminOrigin}/`, `${adminOrigin}/login`]]);
const phase = process.argv[2];
assert.ok(['baseline', 'postdeploy'].includes(phase), 'Usage: node production-readonly.mjs baseline|postdeploy');
const artifacts = join(dirname(fileURLToPath(import.meta.url)), '.artifacts');
await mkdir(artifacts, { recursive: true });
const stamp = Date.now();
const report = { phase, startedAt: new Date().toISOString(), api: [], pages: [], failures: [] };
const client = new ChromeMcp();
const parse = result => JSON.parse(textContent(result).match(/```(?:json)?\s*([\s\S]*?)```/)[1]);

try {
  // Explicit GET only; no production entity bodies or authentication are collected.
  for (const endpoint of ['servicios', 'paquetes', 'galeria', 'promociones']) {
    for (const origin of endpoint === 'galeria' ? [publicOrigin, adminOrigin] : [publicOrigin]) {
      const started = Date.now();
      try {
        const response = await fetch(`${apiOrigin}/api/${endpoint}`, {
          method: 'GET', headers: { Origin: origin }, redirect: 'error', signal: AbortSignal.timeout(45000),
        });
        await response.arrayBuffer();
        const observation = { endpoint, origin, status: response.status, cors: response.headers.get('access-control-allow-origin'), elapsedMs: Date.now() - started };
        report.api.push(observation);
        if (observation.status !== 200 || observation.cors !== origin) report.failures.push({ api: observation });
      } catch (error) {
        const observation = { endpoint, origin, error: error.message, elapsedMs: Date.now() - started };
        report.api.push(observation);
        report.failures.push({ api: observation });
      }
    }
  }
  report.server = (await client.start()).serverInfo;
  for (const [route, url, screenshot] of pages) {
    assert.ok(allowed.has(url), 'Navigation outside allowlist');
    const opened = textContent(await client.call('new_page', { url, isolatedContext: 'azure-production-readonly', timeout: 45000 }));
    const match = opened.match(/^(\d+):.*\[selected\]/m);
    assert.ok(match, opened);
    const pageId = Number(match[1]);
    for (const width of screenshot ? [1440, 390] : [1440]) {
      await client.call('emulate', { pageId, viewport: `${width}x900x1${width === 390 ? ',mobile,touch' : ''}` });
      const started = Date.now();
      let network;
      let dom;
      do {
        dom = parse(await client.call('evaluate_script', {
          pageId, waitForStableDom: false,
          function: `() => ({url:location.href,title:document.title,lang:document.documentElement.lang,
            width:innerWidth,documentWidth:document.documentElement.scrollWidth,fonts:document.fonts.status,
            headings:[...document.querySelectorAll('main h1,main h2')].map(e=>e.textContent.trim()),
            loading:[...document.querySelectorAll('main [role="status"]')].some(e=>/Cargando/.test(e.textContent)),
            images:[...document.images].filter(e=>e.getBoundingClientRect().top<innerHeight).map(e=>({complete:e.complete,loaded:e.naturalWidth>0,alt:e.alt})),
            fields:[...document.querySelectorAll('form input,form select,form textarea')].map(e=>({id:e.id,type:e.type,required:e.required,label:e.labels?.[0]?.textContent.trim()})),
            buttons:[...document.querySelectorAll('form button')].map(e=>({type:e.type,text:e.textContent.trim()})),
            loginLinks:[...document.querySelectorAll('a[href]')].filter(e=>/\\/(login|registro|panel)(\\/|$)/.test(new URL(e.href).pathname)).map(e=>e.textContent.trim())})`,
        }));
        network = textContent(await client.call('list_network_requests', { pageId }));
        if (dom.headings.length && !dom.loading && dom.fonts === 'loaded' && dom.images.every(e => e.complete) && !network.includes('[pending]')) break;
        await new Promise(resolve => setTimeout(resolve, 1000));
      } while (Date.now() - started < 60000);
      assert.equal(dom.url, expectedDestinations.get(url) ?? url, 'Unexpected redirect; do not continue navigation');
      const consoleMessages = textContent(await client.call('list_console_messages', { pageId, types: ['error', 'warn'] }));
      const snapshot = textContent(await client.call('take_snapshot', { pageId }));
      const observation = { route, url, width, dom, consoleMessages, network, snapshot };
      if (screenshot) {
        observation.screenshot = join(artifacts, `${phase}-${stamp}-${route}-${width}.png`);
        await client.call('take_screenshot', { pageId, filePath: observation.screenshot, fullPage: false });
      }
      report.pages.push(observation);
      const problems = [];
      if (!dom.headings.length || dom.loading || dom.fonts !== 'loaded' || dom.images.some(e => !e.loaded)) problems.push('document/assets incomplete');
      if (dom.documentWidth > dom.width) problems.push('horizontal overflow');
      if (url.startsWith(publicOrigin) && dom.loginLinks.length) problems.push('public administrative link');
      if (!consoleMessages.includes('<no console messages found>')) problems.push('console error/warning');
      if (/\[(pending|failed|[45]\d\d)\]/i.test(network)) problems.push('network pending/failure');
      if (/\b(POST|PUT|PATCH|DELETE) https?:/.test(network)) problems.push('unexpected mutating request');
      if (route === 'solicitar') {
        if (dom.fields.find(e => e.id === 'correo')?.required !== false) problems.push('email should be optional');
        if (dom.fields.find(e => e.id === 'telefono')?.required !== true) problems.push('phone should be required');
      }
      if (problems.length) report.failures.push({ route, width, problems });
      console.log(JSON.stringify({ phase, route, width, problems, requests: network.match(/reqid=/g)?.length, screenshot: observation.screenshot }));
    }
  }
} catch (error) {
  report.failures.push({ error: error.message });
} finally {
  await client.close();
  report.finishedAt = new Date().toISOString();
  report.status = report.failures.length ? 'failed' : 'passed';
  const reportPath = join(artifacts, `${phase}-${stamp}.json`);
  await writeFile(reportPath, JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ reportPath, status: report.status, failures: report.failures, api: report.api }));
  if (report.failures.length) process.exitCode = 1;
}

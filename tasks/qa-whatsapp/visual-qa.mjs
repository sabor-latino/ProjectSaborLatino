import assert from 'node:assert/strict';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ChromeMcp, textContent } from './mcp-client.mjs';

const directory = dirname(fileURLToPath(import.meta.url));
const sourcePath = process.argv[2];
if (!sourcePath) throw new Error('Indicar reporte integration passed para capturas sin nuevo POST.');
const source = JSON.parse(await readFile(sourcePath, 'utf8'));
assert.equal(source.status, 'passed');
assert.ok(Number.isInteger(source.requestId));
const body = JSON.parse(source.creationResponse.match(/### Request Body\s*([\s\S]*?)\s*### Response Headers/)[1]);
const restored = {
  id: source.requestId, nombreContacto: body.nombreContacto, correoContacto: body.correoContacto ?? null,
  telefonoContacto: body.telefonoContacto, paqueteId: body.paqueteId ?? null, paqueteNombre: null,
  tipoEvento: body.tipoEvento, fechaEvento: body.fechaEvento, lugar: body.lugar,
  cantidadInvitados: body.cantidadInvitados ?? null, comentarios: body.comentarios,
  estado: 'Pendiente', fechaCreacion: source.startedAt,
};
const publicOrigin = process.env.SABOR_QA_PUBLIC_URL || 'http://localhost:54528';
const adminOrigin = process.env.SABOR_QA_ADMIN_URL || 'http://localhost:54529';
assert.ok(['http://localhost:54526','http://localhost:54528'].includes(publicOrigin));
assert.ok(['http://localhost:54527','http://localhost:54529'].includes(adminOrigin));
const artifacts = join(directory, '.artifacts');
await mkdir(artifacts, { recursive: true });
const stamp = Date.now();
const report = { sourcePath, requestId: source.requestId, restoredTaskStorageForVisualsOnly: true, captures: [] };
const client = new ChromeMcp();
function parse(result) {
  const text = textContent(result);
  return JSON.parse(text.match(/```(?:json)?\s*([\s\S]*?)```/)[1]);
}
async function tree(pageId) { return textContent(await client.call('take_snapshot', { pageId })); }
function element(snapshot, label, role) {
  const line = snapshot.split('\n').find(line => line.includes(`"${label}"`) && line.includes(role));
  assert.ok(line, `No aparece ${role}: ${label}`);
  return line.match(/uid=([^\s]+)/)[1];
}
async function click(pageId, label) {
  const deadline=Date.now()+3000;let snapshot;
  do{snapshot=await tree(pageId);if(snapshot.split('\n').some(line=>line.includes('button')&&line.includes(`"${label}"`)))break;}while(Date.now()<deadline);
  return client.call('click', { pageId, uid: element(snapshot, label, 'button') });
}
async function open(url, context) {
  const text = textContent(await client.call('new_page', { url, isolatedContext: context }));
  return Number(text.match(/^(\d+):.*\[selected\]/m)[1]);
}
async function wait(pageId, text) { await client.call('wait_for', { pageId, text: [text], visible: true, timeout: 20000 }); }
async function capture(pageId, label, expectedText) {
  await wait(pageId, expectedText);
  // Captura neutral: el foco funcional ya se comprobó por separado en el E2E.
  await client.call('evaluate_script', { pageId, function: '() => { document.activeElement?.blur(); window.scrollTo(0,0); return {scrollY}; }', waitForStableDom: false });
  const dom = parse(await client.call('evaluate_script', { pageId, waitForStableDom: false, function: `async () => {
    await Promise.race([Promise.all([document.fonts.ready,...[...document.images].map(img=>img.complete?Promise.resolve():new Promise(resolve=>{img.addEventListener('load',resolve,{once:true});img.addEventListener('error',resolve,{once:true});}))]),new Promise(resolve=>setTimeout(resolve,10000))]);
    await Promise.race([Promise.all([...document.images].map(img=>img.decode().catch(()=>{}))),new Promise(resolve=>setTimeout(resolve,5000))]);
    await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
    const rect = selector => {const el=document.querySelector(selector);if(!el)return null;const r=el.getBoundingClientRect();return {top:r.top+scrollY,bottom:r.bottom+scrollY,height:r.height};};
    return {url:location.href,lang:document.documentElement.lang,width:innerWidth,height:innerHeight,scrollY,documentWidth:document.documentElement.scrollWidth,
      main:rect('main'),section:rect('main>section'),footer:rect('footer'),summary:document.querySelector('#resumen-whatsapp')?.value,
      registration:[...document.querySelectorAll('main dt')].filter(el=>/Registrada|Registered/.test(el.textContent)).map(el=>el.nextElementSibling?.textContent.trim()),
      receipt:[...document.querySelectorAll('.recepcion-whatsapp')].map(el=>el.textContent.trim()),
      overflow:[...document.querySelectorAll('main *')].filter(el=>{const r=el.getBoundingClientRect();return r.width>0&&(r.right>innerWidth+1||r.left< -1);}).slice(0,10).map(el=>({tag:el.tagName,class:el.className,text:el.textContent.trim().slice(0,90)}))};
  }` }));
  const snapshot = await tree(pageId);
  assert.ok(!snapshot.includes('"Cargando…"'), 'No capturar fallback de carga.');
  const png = join(artifacts, `${stamp}-${label}.png`);
  await client.call('take_screenshot', { pageId, filePath: png, fullPage: true });
  report.captures.push({ label, png, dom });
  await writeFile(join(artifacts, `${stamp}-${label}.txt`), snapshot);
  console.log(JSON.stringify({ label, png, dom }));
}
try {
  await client.start();
  const cleanForm = await open(publicOrigin + '/solicitar', 'visual-form');
  await client.call('emulate', { pageId: cleanForm, viewport: '1440x900x1' });
  await capture(cleanForm, 'formulario-1440', 'Su solicitud');

  const page = await open(publicOrigin + '/solicitar', 'visual-recovery');
  await client.call('evaluate_script', { pageId: page, function: `() => {sessionStorage.setItem('sabor_solicitud_whatsapp',${JSON.stringify(JSON.stringify({actor:'visitante',creado:Date.now(),solicitud:restored}))});return {restoredTaskStorage:true};}` });
  await client.call('navigate_page', { pageId: page, type: 'reload' });
  await client.call('emulate', { pageId: page, viewport: '1440x900x1' });
  await capture(page, 'exito-1440', 'Solicitud guardada');
  await click(page, 'Opciones de idioma');
  await click(page, 'Cambiar idioma a inglés (EN)');
  await capture(page, 'exito-en-1440', 'Request saved');
  await click(page, 'Language options');
  await click(page, 'Switch language to Spanish (ES)');
  await click(page, 'Opciones de tamaño de fuente');
  await click(page, 'A++ — Tamaño de letra extra grande');
  await click(page, 'Opciones de contraste');
  await click(page, 'Alto contraste oscuro');
  for (const width of [390,320]) {
    await client.call('emulate', { pageId: page, viewport: `${width}x844x1,mobile,touch` });
    await capture(page, `exito-${width}-a++`, 'Solicitud guardada');
    const last = report.captures.at(-1).dom;
    assert.ok(last.summary.includes(`Solicitud #${source.requestId}`));
    assert.equal(last.documentWidth, width);
  }

  const admin = await open(adminOrigin + '/panel/solicitudes', 'visual-admin');
  await wait(admin, 'Portal Administrativo');
  const login = await tree(admin);
  await client.call('fill_form', { pageId: admin, elements: [
    {uid:element(login,'Correo electrónico','textbox'),value:'superadmin@saborlatino.com'},
    {uid:element(login,'Contraseña','textbox'),value:process.env.SABOR_QA_DEMO_PASSWORD||'SaborLatino!2026'},
  ] });
  await click(admin,'Entrar');
  await wait(admin,source.artificialContact);
  await client.call('emulate',{pageId:admin,viewport:'320x844x1,mobile,touch'});
  await wait(admin,source.artificialContact);
  const requests=await tree(admin);
  const line=requests.split('\n').find(line=>line.includes('button')&&line.includes(`#${source.requestId} ·`));
  assert.ok(line);
  if (!requests.includes('Recepción confirmada manualmente')) await client.call('click',{pageId:admin,uid:line.match(/uid=([^\s]+)/)[1]});
  await capture(admin,'admin-320','Recepción confirmada manualmente');
  for (const [name,pageId] of [['publico',cleanForm],['admin',admin]]) {
    report[name]={console:textContent(await client.call('list_console_messages',{pageId,types:['error','warn']})),network:textContent(await client.call('list_network_requests',{pageId}))};
  }
  report.status='passed';
} catch(error) {
  report.status='failed';report.error=error.message;console.error(error.message);process.exitCode=1;
} finally {
  await writeFile(join(artifacts,`${stamp}-visual.json`),JSON.stringify(report,null,2));
  await client.close();
}

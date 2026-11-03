import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ChromeMcp, textContent } from './mcp-client.mjs';

// Escrituras solo en la demostración local confirmada por el principal.
// No transmite WhatsApp: el servidor Chrome bloquea dominios WhatsApp/wa.me.
if (!process.argv.includes('--demo-confirmada')) throw new Error('Confirmar servidores demo antes de usar --demo-confirmada.');
function portalOrigin(value, ports) {
  const url = new URL(value);
  if (url.protocol !== 'http:' || url.hostname !== 'localhost' || !ports.includes(url.port) ||
      url.username || url.password || url.pathname !== '/' || url.search || url.hash) {
    throw new Error('QA solo acepta origins de portales localhost en puertos demo explícitos.');
  }
  return url.origin;
}
const publicOrigin = portalOrigin(process.env.SABOR_QA_PUBLIC_URL || 'http://localhost:54526', ['54526', '54528']);
const adminOrigin = portalOrigin(process.env.SABOR_QA_ADMIN_URL || 'http://localhost:54527', ['54527', '54529']);
const requestUrl = publicOrigin + '/solicitar';
const artifacts = join(dirname(fileURLToPath(import.meta.url)), '.artifacts');
await mkdir(artifacts, { recursive: true });
const offlineOnly = process.argv.includes('--offline-only');
const client = new ChromeMcp({ blockWhatsApp: !offlineOnly });
const evidence = { startedAt: new Date().toISOString(), steps: [], artificialContact: `QA WhatsApp ${Date.now()}` };
const stamp = Date.now();
let observedPublicPage;

function uid(snapshot, label, role) {
  const matches = snapshot.split('\n').filter(line => line.includes(`"${label}"`) &&
    (role ? line.includes(role) : /\b(textbox|combobox|DateTime|spinbutton)\b/.test(line)));
  const match = matches[0]?.match(/uid=([^\s]+)/);
  if (!match) throw new Error(`No se encontró elemento ${role ?? ''} "${label}". Ver snapshot de evidencia.`);
  return match[1];
}
async function snapshot(pageId, name) {
  const value = textContent(await client.call('take_snapshot', { pageId }));
  await writeFile(join(artifacts, `${stamp}-${name}.txt`), value);
  return value;
}
async function open(url) {
  const output = textContent(await client.call('new_page', { url, isolatedContext: 'sabor-integration', timeout: 30000 }));
  const match = output.match(/^(\d+):.*\[selected\].*$/m);
  assert.ok(match, output);
  return Number(match[1]);
}
async function click(pageId, tree, label) {
  return client.call('click', { pageId, uid: uid(tree, label, 'button') });
}
async function readTaskData(pageId) {
  const output = textContent(await client.call('evaluate_script', {
    pageId, waitForStableDom: false,
    function: `() => ({url:location.href, summary:document.querySelector('#resumen-whatsapp')?.value ?? null,
      whatsapp:[...document.querySelectorAll('a[href]')].filter(el=>el.href.startsWith('https://wa.me/')).map(el=>({text:el.textContent.trim(),href:el.href})),
      width:innerWidth,documentWidth:document.documentElement.scrollWidth,
      lang:document.documentElement.lang,font:document.documentElement.dataset.fontScale,contrast:document.documentElement.dataset.contrast,
      active:{tag:document.activeElement?.tagName,id:document.activeElement?.id,class:document.activeElement?.className,top:document.activeElement?.getBoundingClientRect().top},headerBottom:document.querySelector('header')?.getBoundingClientRect().bottom})`,
  }));
  const json = output.match(/```(?:json)?\s*([\s\S]*?)```/);
  if (!json) throw new Error('Respuesta evaluate_script sin JSON: ' + output);
  return JSON.parse(json[1]);
}

try {
  await client.start();
  const publicPage = await open(requestUrl);
  observedPublicPage = publicPage;
  await client.call('wait_for', { pageId: publicPage, text: ['Datos del evento','Su solicitud'], visible: true, timeout: 20000 });
  if(process.argv.includes('--large')){
    await click(publicPage,await snapshot(publicPage,'antes-a++'),'Opciones de tamaño de fuente');
    let fontTree=await snapshot(publicPage,'fuente-menu');
    for(let i=0;i<10&&!fontTree.includes('A++ — Tamaño de letra extra grande');i++)fontTree=await snapshot(publicPage,'fuente-menu');
    await click(publicPage,fontTree,'A++ — Tamaño de letra extra grande');
  }
  if(process.argv.includes('--mobile'))await client.call('emulate',{pageId:publicPage,viewport:'320x844x1,mobile,touch'});
  await client.call('wait_for', { pageId: publicPage, text: ['Datos del evento','Su solicitud'], visible: true, timeout: 20000 });
  let tree = await snapshot(publicPage, 'formulario');
  const submitName=tree.includes('button "Guardar y abrir WhatsApp"')?'Guardar y abrir WhatsApp':'Solicitar cotización por WhatsApp';
  const commentsName=tree.includes('textbox "Comentarios (opcional)"')?'Comentarios (opcional)':'Comentarios o requerimientos especiales (opcional)';
  await client.call('take_screenshot', { pageId: publicPage, filePath: join(artifacts, `${stamp}-formulario-desktop.png`), fullPage: true });
  await client.call('fill_form', { pageId: publicPage, elements: [
    { uid: uid(tree, 'Nombre completo'), value: evidence.artificialContact },
    { uid: uid(tree, 'Teléfono de contacto'), value: '+506 8888 1111' },
    { uid: uid(tree, 'Tipo de evento'), value: 'Boda' },
    { uid: uid(tree, 'Lugar del evento'), value: 'Salón QA #1 & terraza' },
    { uid: uid(tree, commentsName), value: 'PRUEBA LOCAL QA. Salsa\nMerengue; no enviar mensaje real.' },
  ] });
  // Adaptación documentada: Locator.fill del MCP deja datetime-local vacío en este
  // Chrome. Setter nativo + eventos reproduce la edición del único campo de fecha.
  // Autorizado para QA local; no modifica fuentes ni lee autenticación.
  await client.call('evaluate_script', { pageId: publicPage, function: `() => {
    const el = document.querySelector('#fecha');
    Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(el, '2030-04-20T18:00');
    el.dispatchEvent(new Event('input', {bubbles:true}));
    el.dispatchEvent(new Event('change', {bubbles:true}));
    return {value:el.value,valid:el.validity.valid};
  }` });
  const validity = textContent(await client.call('evaluate_script', { pageId: publicPage, waitForStableDom: false,
    function: `() => ({valid:document.querySelector('form').checkValidity(),fields:[...document.querySelectorAll('form input,form select,form textarea')].map(el=>({id:el.id,type:el.type,pattern:el.getAttribute('pattern'),valid:el.validity.valid,message:el.validationMessage}))})`,
  }));
  evidence.steps.push({ step: 'formulario-valido-adaptador-fecha', validity });
  assert.equal(JSON.parse(validity.match(/```(?:json)?\s*([\s\S]*?)```/)[1]).valid, true, validity);
  tree = await snapshot(publicPage, 'formulario-completo');
  if (offlineOnly) {
  // MCP no permite throttling y blocklist simultáneos. Este proceso solo interactúa
  // con el formulario tras activar Offline; nunca restaura red ni abre enlaces.
  await client.call('emulate', { pageId: publicPage, networkConditions: 'Offline',...(process.argv.includes('--mobile')?{viewport:'320x844x1,mobile,touch'}:{}) });
  await click(publicPage, tree, submitName);
  await client.call('wait_for', { pageId: publicPage, text: ['No se pudo conectar con el servidor', 'No se pudo guardar la solicitud'], visible: true, timeout: 20000 });
  const failedSave = await readTaskData(publicPage);
  assert.equal(failedSave.url, requestUrl);
  assert.equal(failedSave.summary, null);
  assert.ok(failedSave.whatsapp.every(link => !new URL(link.href).searchParams.has('text')), 'El error no debe ofrecer resumen de una solicitud guardada.');
  assert.notEqual(failedSave.active.tag,'BODY','El error debe recibir foco.');
  assert.ok(failedSave.active.top>=failedSave.headerBottom-1,'El foco del error debe quedar visible bajo el header.');
  evidence.steps.push({ step: 'fallo-sin-abrir-whatsapp', result: 'OK; Offline deliberado' });
  tree = await snapshot(publicPage, 'error-sin-conexion');
  await client.call('take_screenshot', { pageId: publicPage, filePath: join(artifacts, `${stamp}-error-sin-conexion.png`), fullPage: true });
  evidence.publico = {
    console: textContent(await client.call('list_console_messages', { pageId: publicPage, types: ['error', 'warn'] })),
    network: textContent(await client.call('list_network_requests', { pageId: publicPage })),
  };
  } else {
  try {
    await click(publicPage, tree, submitName);
  } catch (error) {
    // La salida externa se bloquea a propósito; no se ignoran errores de guardado.
    if (!/BLOCKED|net::ERR|navigation/i.test(error.message)) throw error;
    evidence.steps.push({ step: 'navegacion-externa-bloqueada', note: error.message });
  }
  await snapshot(publicPage, 'despues-submit');
  const submittedNetwork = textContent(await client.call('list_network_requests', { pageId: publicPage, includePreservedRequests: true }));
  evidence.submittedNetwork = submittedNetwork;
  const postLine = submittedNetwork.split('\n').find(line => line.includes('POST ') && /\/api\/solicitudes(?:\s|\?)/.test(line));
  const requestId = postLine?.match(/reqid=(\d+)/)?.[1];
  assert.ok(postLine?.includes('[201]'),'Esperar POST201 antes del retorno.');
  if (requestId) evidence.creationResponse = textContent(await client.call('get_network_request', { pageId: publicPage, reqid: Number(requestId) }));
  // Volver deliberadamente mediante carga nueva: no dependemos de BFCache.
  await client.call('navigate_page', { pageId: publicPage, type: 'url', url: requestUrl });
  await client.call('wait_for', { pageId: publicPage, text: ['Solicitud guardada'], visible: true, timeout: 20000 });
  const saved = await readTaskData(publicPage);
  assert.ok(saved.summary?.includes(evidence.artificialContact), 'El éxito guardado debe corresponder a esta prueba.');
  const reference = saved.summary.match(/Solicitud #(\d+)/)?.[1];
  assert.ok(reference, 'Referencia de solicitud visible en resumen.');
  const url = new URL(saved.whatsapp[0]?.href);
  assert.equal(url.hostname, 'wa.me');
  assert.equal(url.pathname, '/50685262096');
  assert.equal(url.searchParams.get('text'), saved.summary);
  assert.ok(saved.summary.includes('Salón QA #1 & terraza'));
  assert.ok(saved.summary.includes('Salsa\nMerengue'));
  evidence.requestId = Number(reference);
  evidence.steps.push({ step: 'guardar-y-recuperar', taskData: saved });
  tree = await snapshot(publicPage, 'solicitud-guardada');
  assert.ok(tree.includes('Abrir el chat no confirma'), 'Debe explicar que abrir no envía.');
  await client.call('take_screenshot', { pageId: publicPage, filePath: join(artifacts, `${stamp}-exito-desktop.png`), fullPage: true });
  await client.call('navigate_page', { pageId: publicPage, type: 'reload' });
  await client.call('wait_for', { pageId: publicPage, text: ['Solicitud guardada'], visible: true, timeout: 20000 });
  assert.equal((await readTaskData(publicPage)).summary, saved.summary);
  evidence.steps.push({ step: 'recarga-conserva-resumen', result: 'OK' });
  tree = await snapshot(publicPage, 'antes-idioma');
  await click(publicPage, tree, 'Opciones de idioma');
  tree = await snapshot(publicPage, 'idioma-abierto');
  await click(publicPage, tree, 'Cambiar idioma a inglés (EN)');
  await client.call('wait_for', { pageId: publicPage, text: ['Request saved'], visible: true, timeout: 20000 });
  const english = await readTaskData(publicPage);
  assert.equal(english.lang, 'en');
  assert.ok(english.summary.includes(`Request #${reference}`));
  assert.equal(new URL(english.whatsapp[0].href).searchParams.get('text'), english.summary);
  evidence.steps.push({ step: 'ingles-misma-referencia', taskData: english });
  await client.call('take_screenshot', { pageId: publicPage, filePath: join(artifacts, `${stamp}-exito-ingles.png`), fullPage: true });
  tree = await snapshot(publicPage, 'antes-espanol');
  await click(publicPage, tree, 'Language options');
  tree = await snapshot(publicPage, 'idioma-ingles-abierto');
  await click(publicPage, tree, 'Switch language to Spanish (ES)');
  tree = await snapshot(publicPage, 'antes-fuente');
  await click(publicPage, tree, 'Opciones de tamaño de fuente');
  tree = await snapshot(publicPage, 'fuente-abierta');
  await click(publicPage, tree, 'A++ — Tamaño de letra extra grande');
  tree = await snapshot(publicPage, 'antes-contraste');
  await click(publicPage, tree, 'Opciones de contraste');
  tree = await snapshot(publicPage, 'contraste-abierto');
  await click(publicPage, tree, 'Alto contraste oscuro');
  await client.call('emulate', { pageId: publicPage, viewport: '390x844x1,mobile,touch' });
  await client.call('wait_for', { pageId: publicPage, text: ['Solicitud guardada'], visible: true, timeout: 20000 });
  const mobile = await readTaskData(publicPage);
  assert.equal(mobile.summary, saved.summary);
  assert.ok(mobile.documentWidth <= mobile.width, 'No debe desbordar horizontalmente en móvil.');
  await client.call('take_screenshot', { pageId: publicPage, filePath: join(artifacts, `${stamp}-exito-movil.png`), fullPage: true });
  evidence.steps.push({ step: 'movil', taskData: mobile });
  await client.call('emulate', { pageId: publicPage, viewport: '320x800x1,mobile,touch' });
  await client.call('wait_for', { pageId: publicPage, text: ['Solicitud guardada'], visible: true, timeout: 20000 });
  const narrow = await readTaskData(publicPage);
  assert.equal(narrow.summary, saved.summary);
  assert.ok(narrow.documentWidth <= narrow.width, 'No debe desbordar a 320px con A++ y alto contraste.');
  assert.equal(narrow.font, 'extra');
  assert.equal(narrow.contrast, 'alto-oscuro');
  await client.call('take_screenshot', { pageId: publicPage, filePath: join(artifacts, `${stamp}-exito-320-contraste.png`), fullPage: true });
  evidence.steps.push({ step: '320-a++-contraste', taskData: narrow });
  tree=await snapshot(publicPage,'antes-nueva-solicitud');
  await click(publicPage,tree,tree.includes('button "Nueva solicitud"')?'Nueva solicitud':'Enviar otra solicitud');
  await client.call('wait_for',{pageId:publicPage,text:['Su solicitud','Datos del evento'],visible:true,timeout:20000});
  const restarted=await readTaskData(publicPage);
  assert.equal(restarted.active.id,'nombre');
  assert.ok(restarted.active.top>=restarted.headerBottom-1);
  evidence.steps.push({step:'nueva-solicitud-focus-nombre',active:restarted.active});

  const adminPage = await open(adminOrigin + '/panel/solicitudes');
  await client.call('wait_for', { pageId: adminPage, text: ['Portal Administrativo'], visible: true, timeout: 20000 });
  tree = await snapshot(adminPage, 'login');
  await client.call('fill_form', { pageId: adminPage, elements: [
    { uid: uid(tree, 'Correo electrónico'), value: 'superadmin@saborlatino.com' },
    { uid: uid(tree, 'Contraseña'), value: process.env.SABOR_QA_DEMO_PASSWORD || 'SaborLatino!2026' },
  ] });
  tree = await snapshot(adminPage, 'login-listo');
  await click(adminPage, tree, 'Entrar');
  await client.call('wait_for', { pageId: adminPage, text: [evidence.artificialContact], visible: true, timeout: 20000 });
  tree = await snapshot(adminPage, 'panel-solicitudes');
  const requestLine = tree.split('\n').find(line => line.includes('button') && line.includes(`#${reference} ·`));
  assert.ok(requestLine, 'Solicitud creada debe existir en panel.');
  await client.call('click', { pageId: adminPage, uid: requestLine.match(/uid=([^\s]+)/)[1] });
  tree = await snapshot(adminPage, 'detalle');
  assert.ok(tree.includes('Sin confirmación de recepción'));
  await click(adminPage, tree, 'Marcar recibido por WhatsApp');
  await client.call('handle_dialog', { pageId: adminPage, action: 'accept' });
  await client.call('wait_for', { pageId: adminPage, text: ['Recepción confirmada manualmente'], visible: true, timeout: 20000 });
  tree = await snapshot(adminPage, 'recibido');
  const markerFocus=textContent(await client.call('evaluate_script',{pageId:adminPage,waitForStableDom:false,function:`()=>({tag:document.activeElement?.tagName,text:document.activeElement?.textContent?.trim(),top:document.activeElement?.getBoundingClientRect().top,headerBottom:document.querySelector('header')?.getBoundingClientRect().bottom})`}));
  const markerFocusData=JSON.parse(markerFocus.match(/```(?:json)?\s*([\s\S]*?)```/)[1]);
  assert.notEqual(markerFocusData.tag,'BODY','Recepción debe conservar un foco útil al desmontar el botón.');
  assert.ok(markerFocusData.top>=markerFocusData.headerBottom-1,'El foco del aviso debe quedar visible bajo el header.');
  evidence.steps.push({step:'marker-focus',focus:markerFocusData});
  assert.ok(tree.includes('Registrado por'));
  assert.ok(tree.includes('Pendiente'), 'La recepción no debe aceptar el estado comercial.');
  assert.ok(!tree.includes('mailto:null'));
  await client.call('take_screenshot', { pageId: adminPage, filePath: join(artifacts, `${stamp}-panel-recibido.png`), fullPage: true });
  evidence.steps.push({ step: 'recepcion-manual-ui', result: 'OK' });
  for (const [name, pageId] of [['publico', publicPage], ['admin', adminPage]]) {
    evidence[name] = {
      console: textContent(await client.call('list_console_messages', { pageId, types: ['error', 'warn'], includePreservedMessages: true })),
      network: textContent(await client.call('list_network_requests', { pageId, includePreservedRequests: true })),
    };
  }
  }
  evidence.status = 'passed';
  console.log(JSON.stringify({ status: evidence.status, requestId: evidence.requestId, artificialContact: evidence.artificialContact, steps: evidence.steps.map(item => item.step) }, null, 2));
} catch (error) {
  evidence.status = 'failed';
  evidence.error = error.message;
  console.error(error.message);
  if (observedPublicPage !== undefined) {
    try {
      evidence.failureSnapshot = await snapshot(observedPublicPage, 'failure');
      evidence.failureNetwork = textContent(await client.call('list_network_requests', { pageId: observedPublicPage, includePreservedRequests: true }));
    } catch { /* Conservar el fallo inicial si el navegador ya no responde. */ }
  }
  process.exitCode = 1;
} finally {
  await writeFile(join(artifacts, `${stamp}-integration.json`), JSON.stringify(evidence, null, 2));
  await client.close();
}

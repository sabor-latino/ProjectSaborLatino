import assert from 'node:assert/strict';
import {mkdir,writeFile} from 'node:fs/promises';
import {dirname,join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {ChromeMcp,textContent} from './mcp-client.mjs';

const directory=dirname(fileURLToPath(import.meta.url)),artifacts=join(directory,'.artifacts');
await mkdir(artifacts,{recursive:true});
const origin=process.env.SABOR_QA_PUBLIC_URL||'http://localhost:54528';
assert.ok(['http://localhost:54526','http://localhost:54528'].includes(origin));
const quick=process.argv.includes('--quick');
const targetsOnly=process.argv.includes('--targets-only');
const stamp=Date.now(),report={phase:targetsOnly?'focused-final':quick?'preliminary':'final',startedAt:new Date().toISOString(),cases:[],keyboard:[],captures:[]};
const client=new ChromeMcp();
let observedPageId;
function parse(result){return JSON.parse(textContent(result).match(/```(?:json)?\s*([\s\S]*?)```/)[1]);}
async function tree(pageId){return textContent(await client.call('take_snapshot',{pageId}));}
async function clickLabel(pageId,label){
 let line;
 // AX puede actualizarse después de aria-expanded durante la transición de0.2s.
 // Son snapshots observados con deadline, no otro clic ni cambio en la aplicación.
 const deadline=Date.now()+3000;
 do{
  line=(await tree(pageId)).split('\n').find(line=>line.includes('button')&&line.includes(`"${label}"`));
  if(line)break;
 }while(Date.now()<deadline);
 assert.ok(line,`Botón ausente: ${label}`);
 await client.call('click',{pageId,uid:line.match(/uid=([^\s]+)/)[1]});
}
async function ready(pageId){
 await client.call('evaluate_script',{pageId,waitForStableDom:false,function:`async()=>{for(let i=0;i<100;i++){if(document.querySelector('main h1')&&![...document.querySelectorAll('main [role=status]')].some(el=>el.textContent.includes('Cargando')))break;await new Promise(r=>setTimeout(r,100));}await document.fonts.ready;return {ready:!!document.querySelector('main h1')};}`});
}
async function inspect(pageId){return parse(await client.call('evaluate_script',{pageId,sourcePath:join(directory,'audit-page.js'),waitForStableDom:false}));}
try{
 await client.start();
 for(const path of (targetsOnly?['/paquetes']:['/solicitar','/paquetes'])){
  const opened=textContent(await client.call('new_page',{url:origin+path,isolatedContext:'accessibility-public'}));
  const pageId=Number(opened.match(/^(\d+):.*\[selected\]/m)[1]);
  observedPageId=pageId;
  await client.call('emulate',{pageId,viewport:'1440x900x1'});await ready(pageId);
  // Cada configuración se elige por UI en escritorio; emulación móvil puede recargar.
  for(const lang of ['es','en']){
   let data=await inspect(pageId);
   if(data.lang!==lang){await clickLabel(pageId,data.lang==='es'?'Opciones de idioma':'Language options');await clickLabel(pageId,lang==='en'?'Cambiar idioma a inglés (EN)':'Switch language to Spanish (ES)');}
   await clickLabel(pageId,lang==='es'?'Opciones de tamaño de fuente':'Font size options');
   await clickLabel(pageId,lang==='es'?'A++ — Tamaño de letra extra grande':'A++ — Extra large font size');
   for(const mode of (quick||targetsOnly?['estandar']:['estandar','alto-claro','alto-oscuro'])){
    await clickLabel(pageId,lang==='es'?'Opciones de contraste':'Contrast options');
    await clickLabel(pageId,lang==='es'?({'estandar':'Contraste estándar','alto-claro':'Alto contraste claro','alto-oscuro':'Alto contraste oscuro'}[mode]):({'estandar':'Standard contrast','alto-claro':'High contrast light','alto-oscuro':'High contrast dark'}[mode]));
    for(const width of (targetsOnly?[1440,320]:quick?[1440,390,320]:[1440,768,390,320])){
     await client.call('emulate',{pageId,viewport:`${width}x900x1${width<768?',mobile,touch':''}`});await ready(pageId);
     if(path==='/solicitar'&&width===320&&mode==='estandar'){
      await client.call('evaluate_script',{pageId,function:`()=>{const el=document.querySelector('#fecha');Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(el,'2030-04-20T18:00');el.dispatchEvent(new Event('input',{bubbles:true}));el.dispatchEvent(new Event('change',{bubbles:true}));return {value:el.value};}`});
      const date=(await tree(pageId)).split('\n').find(line=>line.includes('DateTime')&&/"(?:Fecha y hora|Date and time)"/.test(line));
      assert.ok(date);
      await client.call('click',{pageId,uid:date.match(/uid=([^\s]+)/)[1]});
      await client.call('press_key',{pageId,key:'ArrowUp'});
      const edited=parse(await client.call('evaluate_script',{pageId,waitForStableDom:false,function:`()=>({value:document.querySelector('#fecha').value,valid:document.querySelector('#fecha').validity.valid})`}));
      assert.ok(edited.value&&edited.value!=='2030-04-20T18:00','Control nativo debe seguir editable con teclado.');
      assert.equal(edited.valid,true);
      await writeFile(join(artifacts,`${stamp}-fecha-${lang}-320.txt`),await tree(pageId));
      await client.call('evaluate_script',{pageId,waitForStableDom:false,function:'()=>{window.scrollTo(0,0);return {scrollY};}'});
      const png=join(artifacts,`${stamp}-fecha-${lang}-320.png`);
      await client.call('take_screenshot',{pageId,filePath:png,fullPage:true});
      report.captures.push({path,lang,mode,width,png,edited});
     }
     data=await inspect(pageId);
     assert.equal(data.font,'extra');assert.equal(data.lang,lang);
     if(targetsOnly){
      assert.ok(data.controls.filter(control=>control.tag==='SUMMARY').every(control=>control.target44),'Disclosure debe medir al menos44px.');
     }
     report.cases.push({path,lang,mode,width,data});
     console.log(JSON.stringify({path,lang,mode,width,documentWidth:data.documentWidth,priceFailures:data.prices.filter(p=>p.clipped||p.overlapsCta).length,labelFailures:data.labelFailures.length}));
    }
    // Preferencias ocultas en menú móvil: volver a escritorio para siguiente selección.
    await client.call('emulate',{pageId,viewport:'1440x900x1'});await ready(pageId);
   }
  }
  if(path==='/solicitar'){
   await client.call('navigate_page',{pageId,type:'url',url:origin+path});await ready(pageId);
   await client.call('press_key',{pageId,key:'Tab'});
   let data=await inspect(pageId);report.keyboard.push({step:'skip-link-focus',data:data.active});
   assert.equal(data.active.tag,'A');
   await client.call('press_key',{pageId,key:'Enter'});
   data=await inspect(pageId);report.keyboard.push({step:'skip-link-target',data:data.active});
   assert.equal(data.active.id,'contenido');
   const snapshot=await tree(pageId);
   const submit=snapshot.split('\n').find(line=>line.includes('button')&&/Save and open WhatsApp|Guardar y abrir WhatsApp|Solicitar cotización por WhatsApp/.test(line));
   assert.ok(submit);
   await client.call('click',{pageId,uid:submit.match(/uid=([^\s]+)/)[1]});
   data=await inspect(pageId);report.keyboard.push({step:'native-empty-invalid-focus',data:data.active});
   assert.equal(data.active.id,'nombre');
   const phoneLine=(await tree(pageId)).split('\n').find(line=>line.includes('textbox')&&/"(?:Contact phone number|Teléfono de contacto)"/.test(line));
   assert.ok(phoneLine);
   await client.call('fill_form',{pageId,elements:[{uid:phoneLine.match(/uid=([^\s]+)/)[1],value:'abc'}]});
   await client.call('press_key',{pageId,key:'Tab'});
   let phone=parse(await client.call('evaluate_script',{pageId,waitForStableDom:false,function:`()=>{const el=document.querySelector('#telefono');return{invalid:el.getAttribute('aria-invalid'),errorId:el.getAttribute('aria-errormessage'),valid:el.validity.valid};}`}));
   report.keyboard.push({step:'phone-invalid-associated',data:phone});assert.equal(phone.invalid,'true');assert.equal(phone.errorId,'telefono-error');
   const corrected=(await tree(pageId)).split('\n').find(line=>line.includes('textbox')&&/"(?:Contact phone number|Teléfono de contacto)"/.test(line));
   await client.call('fill_form',{pageId,elements:[{uid:corrected.match(/uid=([^\s]+)/)[1],value:'+506 8888 1111'}]});
   await client.call('press_key',{pageId,key:'Tab'});
   phone=parse(await client.call('evaluate_script',{pageId,waitForStableDom:false,function:`()=>{const el=document.querySelector('#telefono');return{invalid:el.getAttribute('aria-invalid'),valid:el.validity.valid};}`}));
   report.keyboard.push({step:'phone-corrected',data:phone});assert.equal(phone.valid,true);assert.equal(phone.invalid,null);
  }
 }
 report.findings=report.cases.flatMap(item=>[
  ...(item.data.documentWidth>item.width+1?[{kind:'document-overflow',path:item.path,width:item.width,lang:item.lang,mode:item.mode}]:[]),
  ...item.data.prices.filter(price=>price.clipped||price.overlapsCta).map(price=>({kind:'price-clipping-or-overlap',path:item.path,width:item.width,lang:item.lang,mode:item.mode,price})),
  ...item.data.labelFailures.map(id=>({kind:'missing-label',path:item.path,id})),
 ]);
 report.status=report.findings.length?'findings':'passed';
 if(report.findings.length)process.exitCode=1;
}catch(error){report.status='failed';report.error=error.message;console.error(error.message);process.exitCode=1;
 if(observedPageId!==undefined){try{
  await writeFile(join(artifacts,`${stamp}-accessibility-failure.txt`),await tree(observedPageId));
  report.failureMenus=parse(await client.call('evaluate_script',{pageId:observedPageId,waitForStableDom:false,function:`()=>({scrollY,menus:[...document.querySelectorAll('.accesibilidad-opciones')].map(el=>({text:el.textContent,display:getComputedStyle(el).display,visibility:getComputedStyle(el).visibility,opacity:getComputedStyle(el).opacity,top:el.getBoundingClientRect().top,bottom:el.getBoundingClientRect().bottom}))})`}));
 }catch{}}
}
finally{await writeFile(join(artifacts,`${stamp}-accessibility.json`),JSON.stringify(report,null,2));await client.close();}

import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ChromeMcp, textContent } from './mcp-client.mjs';

const phase=process.argv[2]||'before';
assert.ok(['before','after'].includes(phase));
const origin=process.env.SABOR_QA_PUBLIC_URL||'http://localhost:54528';
assert.ok(['http://localhost:54526','http://localhost:54528'].includes(origin));
const artifacts=join(dirname(fileURLToPath(import.meta.url)),'.artifacts');
await mkdir(artifacts,{recursive:true});
const stamp=Date.now();
const report={phase,startedAt:new Date().toISOString(),captures:[]};
const client=new ChromeMcp();
try{
 await client.start();
 for(const [route,path] of [['inicio','/'],['paquetes','/paquetes'],['galeria','/galeria'],['solicitar','/solicitar']]){
  const opened=textContent(await client.call('new_page',{url:origin+path,isolatedContext:'routes-public'}));
  const pageId=Number(opened.match(/^(\d+):.*\[selected\]/m)[1]);
  for(const width of [1440,390]){
   await client.call('emulate',{pageId,viewport:`${width}x${width===1440?900:844}x1${width===390?',mobile,touch':''}`});
   const output=textContent(await client.call('evaluate_script',{pageId,waitForStableDom:false,function:`async()=>{
    const wait=ms=>new Promise(r=>setTimeout(r,ms));
    for(let i=0;i<100;i++){if(document.querySelector('main h1')&&![...document.querySelectorAll('main [role="status"]')].some(el=>el.textContent.includes('Cargando')))break;await wait(100);}
    ${phase==='after'?"for(let y=0;y<document.documentElement.scrollHeight;y+=Math.max(400,innerHeight-100)){window.scrollTo(0,y);await wait(120);}":''}
    await Promise.race([Promise.all([document.fonts.ready,...[...document.images].map(img=>img.complete?Promise.resolve():new Promise(r=>{img.addEventListener('load',r,{once:true});img.addEventListener('error',r,{once:true});}))]),wait(10000)]);
    await Promise.race([Promise.all([...document.images].map(img=>img.decode().catch(()=>{}))),wait(5000)]);
    window.scrollTo(0,0);
    await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
    return{url:location.href,lang:document.documentElement.lang,width:innerWidth,height:innerHeight,scrollY,documentWidth:document.documentElement.scrollWidth,headings:[...document.querySelectorAll('main h1,main h2')].map(el=>el.textContent.trim()),fields:[...document.querySelectorAll('form label,form legend')].map(el=>el.textContent.trim()),images:[...document.images].map(el=>({loaded:el.complete&&el.naturalWidth>0,alt:el.alt}))};
   }`}));
   const dom=JSON.parse(output.match(/```(?:json)?\s*([\s\S]*?)```/)[1]);
   assert.ok(dom.headings.length>0,'No capturar carga vacía.');
   const prefix=`${phase}-${stamp}-${route}-${width}`;
   const png=join(artifacts,prefix+'.png');
   const snapshot=textContent(await client.call('take_snapshot',{pageId}));
   await writeFile(join(artifacts,prefix+'.txt'),snapshot);
   await client.call('take_screenshot',{pageId,filePath:png,fullPage:true});
   report.captures.push({route,width,png,dom});
   console.log(JSON.stringify({phase,route,width,png,documentWidth:dom.documentWidth}));
  }
 }
 report.status='passed';
}catch(error){report.status='failed';report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{await writeFile(join(artifacts,`${phase}-${stamp}-routes.json`),JSON.stringify(report,null,2));await client.close();}

() => {
  const visible = el => el.checkVisibility({checkOpacity:true,checkVisibilityCSS:true}) && el.getBoundingClientRect().width > 0;
  const rect = el => {const r=el.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height,right:r.right,bottom:r.bottom};};
  const rgba = value => {
    const numbers=value.match(/[\d.]+/g)?.map(Number);
    return numbers?.length>=3 ? [...numbers.slice(0,3),numbers[3]??1] : null;
  };
  const blend = (a,b) => [0,1,2].map(i=>a[i]*a[3]+b[i]*(1-a[3])).concat(1);
  const background = el => {
    if(!el)return {color:[255,255,255,1],uncertain:false};
    const s=getComputedStyle(el),color=rgba(s.backgroundColor)||[0,0,0,0];
    if(color[3]===1 && s.backgroundImage==='none')return {color,uncertain:false};
    const lower=background(el.parentElement);
    // Raster/gradient backgrounds require pixel measurement, not this solid-color calculation.
    return {color:blend(color,lower.color),uncertain:lower.uncertain||s.backgroundImage!=='none'||el.matches('.banda,.portada')};
  };
  const luminance = color => color.slice(0,3).map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4;}).reduce((sum,v,i)=>sum+v*[.2126,.7152,.0722][i],0);
  const ratio = (a,b) => {const x=luminance(a),y=luminance(b);return (Math.max(x,y)+.05)/(Math.min(x,y)+.05);};
  const controls=[...document.querySelectorAll('button,a.boton,summary,input:not([type=hidden]),select,textarea')].filter(visible).map(el=>({
    tag:el.tagName,label:el.getAttribute('aria-label')||el.textContent.trim().slice(0,90)||el.id,
    class:el.className,disabled:el.disabled??false,...rect(el),
    target44:el.getBoundingClientRect().width>=43.9&&el.getBoundingClientRect().height>=43.9,
  }));
  const contrast=[...document.querySelectorAll('main button,main .boton,main label,main legend,main h1,main h2,main p,footer a')].filter(visible).slice(0,160).map(el=>{
    const s=getComputedStyle(el),bg=background(el),fg=rgba(s.color);
    const large=parseFloat(s.fontSize)>=24||(parseFloat(s.fontSize)>=18.6667&&Number(s.fontWeight)>=700);
    const computed=fg?ratio(blend(fg,bg.color),bg.color):null;
    return {text:el.textContent.trim().slice(0,90),class:el.className,fontSize:s.fontSize,fontWeight:s.fontWeight,
      foreground:s.color,background:bg.color,uncertain:bg.uncertain,disabled:el.disabled??false,
      ratio:computed===null?null:Math.round(computed*100)/100,required:large?3:4.5};
  });
  const prices=[...document.querySelectorAll('.paquete__pie')].map(el=>{
    const strong=el.querySelector('.precio strong'),button=el.querySelector('.boton');
    if(!strong||!button)return {invalidMarkup:true};
    const range=document.createRange();range.selectNodeContents(strong);
    const textRects=[...range.getClientRects()].map(r=>({left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width}));
    const box=el.getBoundingClientRect(),card=el.closest('.paquete').getBoundingClientRect(),cta=button.getBoundingClientRect();
    return {name:el.closest('.paquete').querySelector('h3')?.textContent,price:strong.textContent.trim(),
      textRects,footer:rect(el),card:rect(el.closest('.paquete')),button:rect(button),
      clipped:textRects.some(r=>r.left<Math.max(box.left,card.left)-1||r.right>Math.min(box.right,card.right)+1),
      overlapsCta:textRects.some(r=>r.left<cta.right&&r.right>cta.left&&r.top<cta.bottom&&r.bottom>cta.top)};
  });
  const active=document.activeElement,style=active?getComputedStyle(active):null;
  return {
    url:location.href,lang:document.documentElement.lang,font:document.documentElement.dataset.fontScale,
    contrastMode:document.documentElement.dataset.contrast,width:innerWidth,documentWidth:document.documentElement.scrollWidth,
    headings:[...document.querySelectorAll('main h1,main h2,main h3')].map(el=>({tag:el.tagName,text:el.textContent.trim()})),
    active:active?{tag:active.tagName,id:active.id,label:active.getAttribute('aria-label')||active.textContent.trim().slice(0,70),outline:style.outline,rect:rect(active)}:null,
    dateControl:document.querySelector('#fecha')?{value:document.querySelector('#fecha').value,rect:rect(document.querySelector('#fecha')),fontSize:getComputedStyle(document.querySelector('#fecha')).fontSize,scrollWidth:document.querySelector('#fecha').scrollWidth,clientWidth:document.querySelector('#fecha').clientWidth}:null,
    controls,contrast,prices,
    labelFailures:[...document.querySelectorAll('input:not([type=hidden]),select,textarea')].filter(visible).filter(el=>!el.labels?.length&&!el.getAttribute('aria-label')&&!el.getAttribute('aria-labelledby')).map(el=>el.id),
  };
}

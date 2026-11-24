async page => {
  const results=[];
  for(const width of [1440,1280,1060,768,390,320]){
    await page.setViewportSize({width,height:900});
    if(width<=1050)await page.getByRole('button',{name:'Mở menu',exact:true}).click();
    const result=await page.locator('.nav-chevron').evaluate(el=>{
      const icon=el.getBoundingClientRect(),text=el.previousElementSibling.getBoundingClientRect();
      return {offset:Math.abs(icon.y+icon.height/2-text.y-text.height/2),overflow:document.documentElement.scrollWidth>innerWidth};
    });
    if(result.offset>1 || result.overflow)throw Error(JSON.stringify({width,...result}));
    results.push({width,...result});
    if(width<=1050)await page.getByRole('button',{name:'Mở menu',exact:true}).click();
  }
  await page.setViewportSize({width:1440,height:900});
  await page.locator('.header-wrap').screenshot({path:'web/header-aligned.png'});
  return results;
}

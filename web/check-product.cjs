async page => {
  const base='http://127.0.0.1:4173', errors=[];
  page.on('pageerror',error=>errors.push(error.message));
  const list=await (await page.request.get(base+'/api/products?size=3')).json();
  const product=await (await page.request.get(base+'/api/products/'+list.items[0].id)).json();
  const url=base+'/products/'+product.id;
  await page.goto(base+'/catalog');
  await page.locator(`.catalog-grid a[href="/products/${product.id}"]`).click();
  await page.waitForURL(url);
  await page.getByRole('heading',{name:product.name,exact:true}).waitFor();
  if(!(await page.locator('.detail-specifications').innerText()).includes(product.sku))throw Error('SKU mismatch');
  if(!(await page.locator('.detail-price').innerText()).includes(new Intl.NumberFormat('vi-VN',{style:'currency',currency:'VND',maximumFractionDigits:0}).format(product.price)))throw Error('Price mismatch');
  await page.getByRole('button',{name:'Ảnh tiếp theo',exact:true}).click();
  if(await page.getByRole('button',{name:'Xem ảnh Toàn cảnh'}).getAttribute('aria-pressed')!=='true')throw Error('Next photo');
  await page.locator('.gallery-stage').focus();await page.keyboard.press('ArrowRight');
  if(await page.getByRole('button',{name:'Xem ảnh Chi tiết phím'}).getAttribute('aria-pressed')!=='true')throw Error('Keyboard photo');
  await page.getByRole('button',{name:'Phóng to ảnh',exact:true}).click();
  await page.locator('.product-lightbox[open]').waitFor();await page.keyboard.press('ArrowLeft');
  await page.keyboard.press('Escape');
  if(await page.locator('.product-lightbox[open]').count())throw Error('Escape');
  if(!await page.getByRole('button',{name:'Phóng to ảnh',exact:true}).evaluate(e=>e===document.activeElement))throw Error('Focus restoration');
  await page.emulateMedia({reducedMotion:'reduce'});
  if(await page.locator('.gallery-layer').first().evaluate(e=>getComputedStyle(e).transitionDuration)!=='0s')throw Error('Reduced motion');
  for(const width of [2560,1440,1024,768,390,320]){
    await page.setViewportSize({width,height:900});
    if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);
  }
  await page.setViewportSize({width:1440,height:1000});
  await page.evaluate(async()=>{await document.fonts.ready;document.querySelectorAll('img').forEach(i=>i.loading='eager');await Promise.all([...document.images].map(i=>i.decode().catch(()=>{})));});
  if(await page.locator('img').evaluateAll(es=>es.some(e=>!e.naturalWidth)))throw Error('Image failed');
  await page.screenshot({path:'.playwright-cli/product-desktop.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:'.playwright-cli/product-mobile.png',fullPage:true});
  await page.reload();await page.getByRole('heading',{name:product.name,exact:true}).waitFor();
  await page.goto(base+'/products/2147483647');await page.getByRole('heading',{name:'Không tìm thấy sản phẩm'}).waitFor();
  await page.goto(base+'/products/invalid');await page.getByRole('heading',{name:'Không tìm thấy sản phẩm'}).waitFor();
  await page.route('**/api/products/'+product.id,route=>route.fulfill({status:503,body:'Unavailable'}));
  await page.goto(url);await page.getByRole('heading',{name:'Chưa tải được sản phẩm'}).waitFor();
  await page.unroute('**/api/products/'+product.id);await page.getByRole('button',{name:'Thử lại',exact:true}).click();
  await page.getByRole('heading',{name:product.name,exact:true}).waitFor();
  await page.route('**/api/products/'+product.id,route=>route.fulfill({json:{...product,stock:0,isHotSwappable:null,description:''}}));
  await page.reload();await page.getByRole('button',{name:'Tạm hết hàng',exact:true}).waitFor();
  if(!await page.getByRole('button',{name:'Tạm hết hàng',exact:true}).isDisabled())throw Error('Sold-out action');
  await page.getByText('Cửa hàng chưa bổ sung mô tả cho sản phẩm này.').waitFor();
  await page.unroute('**/api/products/'+product.id);
  await page.goto(url);await page.getByRole('heading',{name:product.name,exact:true}).waitFor();
  if(errors.length)throw Error(errors.join(';'));
  return {passed:true,checks:['catalog navigation','API name/sku/price','gallery arrows/thumbnails','keyboard','lightbox/Escape/focus','reduced motion','images','reload','404/invalid ID','503/retry','mock sold out/null/empty'],widths:[2560,1440,1024,768,390,320],errors};
}

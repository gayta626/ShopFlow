async page => {
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto('http://127.0.0.1:4173/');
  await page.locator('#products .product-card').first().waitFor();
  const api = await page.request.get('http://127.0.0.1:4173/api/products?size=3&sort=newest');
  const data = await api.json();
  const names = await page.locator('#products .product-card h3').allTextContents();
  if (JSON.stringify(names) !== JSON.stringify(data.items.map(p => p.name))) throw Error('API/product mismatch');
  for (const width of [1920,1440,1024,768,390,320]) {
    await page.setViewportSize({width,height:900});
    if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);
  }
  await page.setViewportSize({width:1440,height:1000});
  await page.getByRole('button',{name:'Tất cả sản phẩm',exact:true}).first().hover();
  await page.locator('#catalog-menu').waitFor({state:'visible'});
  await page.keyboard.press('Escape');
  await page.getByRole('textbox',{name:'Tìm bàn phím'}).fill('NO-MATCH-SHOPFLOW');
  await page.getByRole('button',{name:'Tìm kiếm',exact:true}).click();
  await page.getByRole('heading',{name:'Chưa có bàn phím phù hợp'}).waitFor();
  await page.getByRole('link',{name:'Xóa tất cả',exact:true}).click();
  await page.goto('http://127.0.0.1:4173/');
  await page.locator('#products .product-open').first().click();
  await page.waitForURL(/\/products\/\d+/);await page.locator('.detail-summary h1').waitFor();
  await page.goto('http://127.0.0.1:4173/');await page.locator('#products .product-card').first().waitFor();
  await page.route('**/api/products?**', route=>route.fulfill({status:503,body:'Unavailable'}));
  await page.reload();
  await page.getByRole('heading',{name:'Chưa thể tải sản phẩm'}).waitFor();
  await page.unroute('**/api/products?**');
  await page.getByRole('button',{name:'Thử lại',exact:true}).click();
  await page.locator('#products .product-card').first().waitFor();
  await page.screenshot({path:'.playwright-cli/react-home-desktop.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});
  await page.getByRole('button',{name:'Mở menu',exact:true}).click();
  await page.locator('.main-nav').getByRole('button',{name:'Tất cả sản phẩm',exact:true}).click();
  await page.locator('#catalog-menu').waitFor({state:'visible'});
  if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Mobile menu overflow');
  await page.screenshot({path:'.playwright-cli/react-menu-mobile.png'});
  await page.getByRole('button',{name:'Đóng ×',exact:true}).click();
  await page.getByRole('button',{name:'Mở menu',exact:true}).click();
  await page.screenshot({path:'.playwright-cli/react-home-mobile.png',fullPage:true});
  await page.setViewportSize({width:1440,height:1000});
  await page.goto('http://127.0.0.1:4173/');
  if(errors.length)throw Error(errors.join(';'));
  return {passed:true,apiProducts:names,widths:[1920,1440,1024,768,390,320],checks:['API match','search empty/reset','menu/Escape/mobile','quick information','503/retry recovery','overflow'],errors};
}

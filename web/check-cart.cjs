async page => {
  const base='http://127.0.0.1:4173', errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  const product=(await (await page.request.get(base+'/api/products?size=1')).json()).items[0];
  if((await page.request.get(base+'/api/cart/items')).status()!==401)throw Error('Public cart must be 401');
  let items=[], role='Customer', failure=0, posts=0;
  const total=()=>({items:items.map(x=>({...x,lineTotal:x.unitPrice*x.quantity})),totalAmount:items.reduce((s,x)=>s+x.unitPrice*x.quantity,0)});
  const item=qty=>({productId:product.id,sku:product.sku,productName:product.name,quantity:qty,unitPrice:product.price,stock:10,isAvailable:true});
  await page.route('**/api/auth/login',route=>route.fulfill({json:{accessToken:'cart-ui-test-only',expiresAt:new Date(Date.now()+60000).toISOString(),user:{id:999,email:'cart@example.invalid',fullName:'Khách kiểm tra',role}}}));
  await page.route('**/api/cart/items**',route=>{
    const req=route.request();if(req.headers().authorization!=='Bearer cart-ui-test-only')throw Error('Missing bearer');
    if(failure)return route.fulfill({status:failure,json:{code:'CONCURRENCY_CONFLICT'}});
    if(req.method()==='POST'){posts++;const body=req.postDataJSON();if(body.productId!==product.id)throw Error('Product ID');items=[item((items[0]?.quantity||0)+body.quantity)];}
    if(req.method()==='PUT')items=[item(req.postDataJSON().quantity)];
    if(req.method()==='DELETE')items=[];
    return route.fulfill({json:total()});
  });
  async function login(){await page.getByLabel('Email',{exact:true}).fill('cart@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('Fake-password-123');await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();}
  await page.goto(base+'/cart');await page.getByRole('heading',{name:'Đăng nhập để xem giỏ hàng'}).waitFor();
  await page.getByRole('link',{name:'Đăng nhập',exact:true}).click();await login();await page.getByRole('heading',{name:'Giỏ hàng đang trống.'}).waitFor();
  await page.getByRole('link',{name:'Khám phá bàn phím',exact:true}).click();await page.locator(`.catalog-grid a[href="/products/${product.id}"]`).click();
  await page.getByLabel('Số lượng thêm vào giỏ').fill('2');await page.getByRole('button',{name:'Thêm vào giỏ hàng',exact:true}).click();await page.getByRole('status').filter({hasText:'Đã thêm sản phẩm'}).waitFor();
  if(posts!==1)throw Error('Duplicate add');
  await page.getByRole('link',{name:'Xem giỏ hàng',exact:true}).click();await page.locator('.cart-row').waitFor();
  await page.getByLabel(`Số lượng ${product.name}`,{exact:true}).fill('3');await page.getByRole('button',{name:'Cập nhật',exact:true}).click();await page.getByText('Đã cập nhật số lượng.',{exact:true}).waitFor();
  if(items[0].quantity!==3)throw Error('Update quantity');
  for(const width of [1440,1024,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);}
  await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:'.playwright-cli/cart-desktop.png',fullPage:true});await page.setViewportSize({width:390,height:844});await page.screenshot({path:'.playwright-cli/cart-mobile.png',fullPage:true});
  failure=409;await page.getByLabel(`Số lượng ${product.name}`,{exact:true}).fill('4');await page.getByRole('button',{name:'Cập nhật',exact:true}).click();await page.getByRole('alert').waitFor();
  if(!await page.getByRole('button',{name:'Cập nhật',exact:true}).isDisabled())throw Error('Stale mutation');
  failure=0;await page.getByRole('button',{name:'Tải lại giỏ',exact:true}).click();await page.locator('.cart-row').waitFor();
  if(await page.getByLabel(`Số lượng ${product.name}`,{exact:true}).inputValue()!=='3')throw Error('Resync');
  items[0].isAvailable=false;items[0].stock=0;await page.getByRole('button',{name:'Làm mới giá và tồn kho'}).click();await page.getByText('Chưa đủ điều kiện mua: thiếu tồn kho hoặc đã ngừng bán.').waitFor();
  await page.getByRole('button',{name:'Xóa',exact:true}).click();await page.getByRole('button',{name:'Giữ lại',exact:true}).click();if(!items.length)throw Error('Cancel delete');
  await page.getByRole('button',{name:'Xóa',exact:true}).click();await page.getByRole('button',{name:'Xóa sản phẩm',exact:true}).click();await page.getByRole('heading',{name:'Giỏ hàng đang trống.'}).waitFor();
  failure=503;await page.getByRole('link',{name:'Tiếp tục mua sắm'}).click();await page.getByRole('link',{name:'Giỏ hàng',exact:true}).click();await page.getByRole('alert').waitFor();failure=0;await page.getByRole('button',{name:'Tải lại giỏ'}).click();await page.getByRole('heading',{name:'Giỏ hàng đang trống.'}).waitFor();
  failure=401;await page.getByRole('link',{name:'Tiếp tục mua sắm'}).click();await page.getByRole('link',{name:'Giỏ hàng',exact:true}).click();await page.getByRole('heading',{name:'Đăng nhập để xem giỏ hàng'}).waitFor();
  failure=0;role='Admin';await page.getByRole('link',{name:'Đăng nhập',exact:true}).click();await login();await page.getByRole('heading',{name:'Giỏ hàng dành cho khách hàng'}).waitFor();
  await page.unroute('**/api/auth/login');await page.unroute('**/api/cart/items**');await page.goto(base+'/cart');
  if(errors.length)throw Error(errors.join(';'));
  return {passed:true,real:['GET cart without token 401'],mock:['login return cart','add quantity/bearer','update','conflict reload','unavailable','delete confirm/cancel','empty','503 retry','401 logout','Admin gate'],widths:[1440,1024,768,390,320],errors};
}

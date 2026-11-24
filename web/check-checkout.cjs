async page => {
  const base='http://127.0.0.1:4173', errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  let mode='success', available=true, empty=false, role='Customer', posts=0;
  const items=[{productId:1,sku:'DEMO-UI',productName:'Bàn phím kiểm tra',quantity:2,unitPrice:1500000,lineTotal:3000000,stock:10,isAvailable:true}];
  const order={id:246,status:'Pending',shippingAddress:'Địa chỉ thử nghiệm trên giao diện',createdAt:new Date().toISOString(),items,totalAmount:3000000};
  await page.route('**/api/auth/login',r=>r.fulfill({json:{accessToken:'checkout-ui-only',expiresAt:new Date(Date.now()+60000).toISOString(),user:{id:1,fullName:'Khách kiểm tra',email:'ui@example.invalid',role}}}));
  await page.route('**/api/cart/items',r=>r.fulfill({json:{items:empty?[]:items.map(i=>({...i,isAvailable:available})),totalAmount:empty?0:3000000}}));
  await page.route('**/api/orders',async r=>{
    posts++;const body=r.request().postDataJSON();
    if(Object.keys(body).join(',')!=='shippingAddress'||r.request().headers().authorization!=='Bearer checkout-ui-only')throw Error('Checkout contract');
    if(mode==='network')return r.abort('failed');
    if(mode==='401')return r.fulfill({status:401});
    if(mode==='409')return r.fulfill({status:409,json:{code:'INSUFFICIENT_STOCK'}});
    if(mode==='500')return r.fulfill({status:500});
    return r.fulfill({status:201,json:{...order,shippingAddress:body.shippingAddress}});
  });
  async function enter(){
    await page.goto(base+'/checkout');await page.getByRole('link',{name:'Đăng nhập',exact:true}).click();
    await page.getByLabel('Email',{exact:true}).fill('ui@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('test-only');await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();
    await page.waitForURL('**/checkout');
  }
  await enter();await page.getByLabel('Địa chỉ giao hàng',{exact:true}).waitFor();
  await page.getByRole('radio',{name:/Thẻ tín dụng/}).check();
  await page.locator('.payment-card-preview').waitFor();
  if(!await page.getByRole('button',{name:'Thanh toán thẻ — sắp hỗ trợ'}).isDisabled())throw Error('Card enabled without provider');
  if(posts!==0)throw Error('Card selection submitted order');
  await page.getByRole('radio',{name:/Đặt hàng demo/}).check();
  for(const width of [1440,1024,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);}
  await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:'.playwright-cli/checkout-desktop.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:'.playwright-cli/checkout-mobile.png',fullPage:true});
  await page.getByLabel('Địa chỉ giao hàng',{exact:true}).fill('          ');await page.getByRole('button',{name:'Xác nhận đặt hàng',exact:true}).click();await page.getByRole('alert').filter({hasText:'10 đến 500'}).waitFor();if(posts!==0)throw Error('Invalid submit');
  await page.getByLabel('Địa chỉ giao hàng',{exact:true}).fill('  Địa chỉ thử nghiệm trên giao diện  ');
  await page.getByRole('button',{name:'Xác nhận đặt hàng',exact:true}).click();await page.getByRole('heading',{name:'Đã tạo đơn hàng.'}).waitFor();if(posts!==1)throw Error('Duplicate');
  await page.getByText('Đơn #246',{exact:true}).waitFor();await page.getByText('Địa chỉ thử nghiệm trên giao diện',{exact:true}).waitFor();
  await page.getByRole('link',{name:'Giỏ hàng',exact:true}).click();await page.getByRole('button',{name:'Tiến hành đặt hàng',exact:true}).click();await page.getByRole('heading',{name:'Đã tạo đơn hàng.'}).waitFor();if(posts!==1)throw Error('Repeat POST on return');
  mode='409';await enter();await page.getByLabel('Địa chỉ giao hàng',{exact:true}).fill(order.shippingAddress);await page.getByRole('button',{name:'Xác nhận đặt hàng',exact:true}).click();await page.getByRole('alert').filter({hasText:'không đủ tồn kho'}).waitFor();if(!await page.getByRole('button',{name:'Xác nhận đặt hàng',exact:true}).isDisabled())throw Error('Conflict not blocked');
  mode='network';await enter();await page.getByLabel('Địa chỉ giao hàng',{exact:true}).fill(order.shippingAddress);await page.getByRole('button',{name:'Xác nhận đặt hàng',exact:true}).click();await page.getByRole('alert').filter({hasText:'Chưa xác nhận được kết quả'}).waitFor();const previous=posts;
  await page.getByRole('link',{name:'Giỏ hàng',exact:true}).click();await page.getByRole('button',{name:'Tiến hành đặt hàng',exact:true}).click();await page.getByRole('alert').filter({hasText:'Chưa xác nhận được kết quả'}).waitFor();if(posts!==previous)throw Error('Retry uncertain POST');
  mode='401';await enter();await page.getByLabel('Địa chỉ giao hàng',{exact:true}).fill(order.shippingAddress);await page.getByRole('button',{name:'Xác nhận đặt hàng',exact:true}).click();await page.getByRole('heading',{name:'Đăng nhập để đặt hàng'}).waitFor();
  mode='success';available=false;await enter();await page.getByLabel('Địa chỉ giao hàng',{exact:true}).waitFor();if(!await page.getByRole('button',{name:'Xác nhận đặt hàng',exact:true}).isDisabled())throw Error('Unavailable');
  available=true;empty=true;await enter();await page.getByRole('heading',{name:'Giỏ hàng đang trống.'}).waitFor();
  empty=false;role='Admin';await enter();await page.getByRole('heading',{name:'Đặt hàng dành cho khách hàng'}).waitFor();
  await page.unroute('**/api/auth/login');await page.unroute('**/api/cart/items');await page.unroute('**/api/orders');await page.goto(base+'/checkout');
  if(errors.length)throw Error(errors.join(';'));
  return {passed:true,source:'network mock; no database writes',checks:['address trim/validation','POST contract/Bearer','201 result','return does not repost','409','network uncertain/no retry','401','unavailable','empty','Admin'],widths:[1440,1024,768,390,320]};
}

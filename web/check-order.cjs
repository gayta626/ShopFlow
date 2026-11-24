async page => {
  const base='http://127.0.0.1:4173', errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  let status='Pending', mode='', role='Customer', posts=0;
  const order=()=>({id:246,status,createdAt:'2026-11-21T03:00:00Z',shippingAddress:'123 Đường Nguyễn Huệ, Thành phố Hồ Chí Minh',totalAmount:3000000,items:[{productId:1,sku:'KEYBOARD-DEMO',productName:'Bàn phím cơ 75% — phiên bản thử nghiệm',quantity:2,unitPrice:1500000,lineTotal:3000000}]});
  await page.route('**/api/auth/login',r=>r.fulfill({json:{accessToken:'order-ui-only',expiresAt:new Date(Date.now()+600000).toISOString(),user:{id:1,fullName:'Khách kiểm tra',email:'ui@example.invalid',role}}}));
  await page.route('**/api/orders/246**',r=>{
    if(r.request().headers().authorization!=='Bearer order-ui-only')throw Error('Missing auth');
    if(r.request().method()==='POST'){
      posts++;
      if(r.request().postData())throw Error('Unexpected payload');
      if(mode==='network')return r.abort('failed');
      if(mode==='409')return r.fulfill({status:409,json:{code:'INVALID_ORDER_STATUS'}});
      status=r.request().url().endsWith('/cancel')?'Cancelled':'Completed';
    }
    if(mode==='404'||mode==='401')return r.fulfill({status:Number(mode)});
    return r.fulfill({json:order()});
  });
  async function enter(){
    await page.goto(base+'/orders/246');await page.getByRole('link',{name:'Đăng nhập',exact:true}).click();
    await page.getByLabel('Email',{exact:true}).fill('ui@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('test-only');await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();await page.waitForURL('**/orders/246');
  }
  await enter();await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).waitFor();
  for(const width of [1440,1024,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);}
  await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:'.playwright-cli/order-desktop.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:'.playwright-cli/order-mobile.png',fullPage:true});
  await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).click();await page.getByRole('button',{name:'Quay lại',exact:true}).click();if(posts)throw Error('Cancel dialog posted');
  await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).click();await page.getByRole('button',{name:'Xác nhận',exact:true}).click();await page.getByText('Đã hủy đơn hàng.',{exact:true}).waitFor();if(posts!==1)throw Error('Duplicate POST');
  status='Shipped';await page.getByRole('button',{name:'Làm mới',exact:true}).click();await page.getByRole('button',{name:'Đã nhận hàng',exact:true}).click();await page.getByRole('button',{name:'Xác nhận',exact:true}).click();await page.getByText('Đã xác nhận nhận hàng.',{exact:true}).waitFor();
  for(const failure of ['409','network']){
    status='Pending';mode='';await page.getByRole('button',{name:'Làm mới',exact:true}).click();await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).waitFor();mode=failure;
    await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).click();await page.getByRole('button',{name:'Xác nhận',exact:true}).click();await page.getByRole('alert').waitFor();if(!await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).isDisabled())throw Error('Uncertain action enabled');
    mode='';await page.getByRole('button',{name:'Tải lại đơn',exact:true}).click();await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).waitFor();
  }
  mode='404';await page.getByRole('button',{name:'Làm mới',exact:true}).click();await page.getByRole('alert').filter({hasText:'Không tìm thấy'}).waitFor();
  mode='401';await page.getByRole('button',{name:'Tải lại đơn',exact:true}).click();await page.getByRole('heading',{name:'Đăng nhập để xem đơn hàng'}).waitFor();
  mode='';role='Admin';await enter();await page.getByRole('heading',{name:'Đơn mua hàng dành cho khách hàng'}).waitFor();
  await page.unroute('**/api/auth/login');await page.unroute('**/api/orders/246**');await page.goto(base+'/orders/246');
  if(errors.length)throw Error(errors.join(';'));
  console.log('PASS order detail, cancel/complete, conflict/network, auth, responsive; writes mocked');
}

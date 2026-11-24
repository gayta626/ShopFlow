async page=>{
  const base='http://127.0.0.1:4173',errors=[];page.on('pageerror',e=>errors.push(e.message));
  let role='Admin',status='Pending',mode='',writes=0,reads=0;
  const order=()=>({id:12,status,createdAt:'2026-11-21T03:00:00Z',shippingAddress:'123 Nguyễn Huệ, Thành phố Hồ Chí Minh',totalAmount:1500000,items:[{productId:1,sku:'UI-TEST',productName:'Bàn phím cơ 75%',quantity:1,unitPrice:1500000,lineTotal:1500000}]});
  await page.route('**/api/auth/login',r=>r.fulfill({json:{accessToken:'admin-ui-only',expiresAt:new Date(Date.now()+600000).toISOString(),user:{id:1,role,email:'ui@example.invalid',fullName:'Quản trị thử nghiệm'}}}));
  await page.route('**/api/admin/orders**',r=>{
    if(r.request().headers().authorization!=='Bearer admin-ui-only')throw Error('Auth missing');
    if(r.request().method()==='PUT'){
      writes++;if(mode==='network')return r.abort('failed');if(mode==='409')return r.fulfill({status:409});
      const body=r.request().postDataJSON();if(Object.keys(body).join(',')!=='status')throw Error('Bad body');status=body.status;
      return r.fulfill({json:order()});
    }
    reads++;if(['401','403','404','503'].includes(mode))return r.fulfill({status:Number(mode)});
    const url=new URL(r.request().url());if(url.pathname==='/api/admin/orders'){
      const p=Number(url.searchParams.get('page')),filter=url.searchParams.get('status');
      return r.fulfill({json:{items:filter==='Completed'?[]:[{...order(),id:p===2?13:12}],page:p,size:10,totalCount:filter==='Completed'?0:11}});
    }
    return r.fulfill({json:order()});
  });
  async function enter(path='/admin/orders'){
    await page.goto(base+path);await page.getByRole('link',{name:'Đăng nhập',exact:true}).click();await page.getByLabel('Email',{exact:true}).fill('ui@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('test-only');await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();await page.waitForURL('**'+path);
  }
  await enter();await page.getByRole('link',{name:'Xem đơn #12',exact:true}).waitFor();
  await page.getByRole('button',{name:'Tiếp',exact:true}).click();await page.getByRole('link',{name:'Xem đơn #13',exact:true}).waitFor();
  await page.getByLabel('Trạng thái',{exact:true}).selectOption('Completed');await page.getByText('Chưa có đơn hàng ở trạng thái này.',{exact:true}).waitFor();
  await page.getByLabel('Trạng thái',{exact:true}).selectOption('');await page.getByRole('link',{name:'Xem đơn #12',exact:true}).waitFor();
  for(const width of [1440,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('List overflow');}
  await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:'.playwright-cli/admin-orders.png',fullPage:true});
  await page.getByRole('link',{name:'Xem đơn #12',exact:true}).click();await page.getByRole('button',{name:'Xác nhận đã thanh toán',exact:true}).click();await page.getByRole('button',{name:'Quay lại',exact:true}).click();if(writes)throw Error('Dismiss wrote');
  await page.getByRole('button',{name:'Xác nhận đã thanh toán',exact:true}).click();await page.getByRole('button',{name:'Xác nhận',exact:true}).click();await page.getByRole('button',{name:'Bắt đầu giao hàng',exact:true}).waitFor();
  for(const width of [1440,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Detail overflow');}
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:'.playwright-cli/admin-order-mobile.png',fullPage:true});
  await page.getByRole('button',{name:'Bắt đầu giao hàng',exact:true}).click();await page.getByRole('button',{name:'Xác nhận',exact:true}).click();await page.getByText('Khách hàng sẽ xác nhận hoàn tất khi đã nhận hàng.',{exact:true}).waitFor();
  status='Pending';await page.getByRole('button',{name:'Tải lại',exact:true}).click();await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).click();await page.getByRole('button',{name:'Xác nhận',exact:true}).click();await page.getByText('Không có thao tác trạng thái tiếp theo.',{exact:true}).waitFor();
  for(const failure of ['409','network']){
    status='Pending';mode='';await page.getByRole('button',{name:'Tải lại',exact:true}).click();await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).waitFor();mode=failure;await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).click();await page.getByRole('button',{name:'Xác nhận',exact:true}).click();await page.getByRole('alert').waitFor();if(!await page.getByRole('button',{name:'Hủy đơn hàng',exact:true}).isDisabled())throw Error('Retry enabled');
  }
  mode='404';await page.getByRole('button',{name:'Tải lại',exact:true}).click();await page.getByRole('alert').filter({hasText:'Không tìm thấy'}).waitFor();
  mode='401';await page.getByRole('button',{name:'Tải lại',exact:true}).click();await page.getByRole('heading',{name:'Đăng nhập để quản lý đơn hàng'}).waitFor();
  mode='';role='Customer';const before=reads;await enter();await page.getByRole('heading',{name:'Khu vực dành cho quản trị viên'}).waitFor();if(reads!==before)throw Error('Customer accessed admin API');
  await page.unroute('**/api/auth/login');await page.unroute('**/api/admin/orders**');await page.goto(base+'/admin/orders');if(errors.length)throw Error(errors.join(';'));
  console.log('PASS admin list/filter/paging/detail/status/confirmation/conflict/network/auth/responsive; mocked writes');
}

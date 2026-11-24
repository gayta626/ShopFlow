async page => {
  const base='http://127.0.0.1:4173', errors=[];
  await page.setViewportSize({width:1440,height:900});
  page.on('pageerror',e=>errors.push(e.message));
  let mode='invalid', meStatus=200, expires=600000, role='Customer', registerStatus=409, putStatus=200, writes=0, fullName='Khách kiểm tra', email='ui@example.invalid';
  await page.route('**/api/**',r=>r.fulfill({status:503}));
  await page.route('**/api/auth/login',r=>mode==='invalid'?r.fulfill({status:401}):mode==='error'?r.fulfill({status:503}):r.fulfill({json:{accessToken:'auth-ui-only',expiresAt:new Date(Date.now()+expires).toISOString(),user:{id:123,email:'ui@example.invalid',fullName:'Khách kiểm tra',role}}}));
  await page.route('**/api/auth/me',async r=>{
    if(r.request().method()==='PUT'){
      writes++;
      if(r.request().headers().authorization!=='Bearer auth-ui-only')throw Error('Missing bearer');
      const body=r.request().postDataJSON();
      if(Object.keys(body).sort().join(',')!=='email,fullName')throw Error('Unexpected profile fields');
      if(putStatus===0)return r.abort();
      if(putStatus!==200)return r.fulfill({status:putStatus});
      fullName=body.fullName;email=body.email;
    }
    return r.fulfill({status:meStatus,json:{id:123,role,fullName,email}});
  });
  await page.route('**/api/auth/register',r=>r.fulfill({status:registerStatus,json:{email:'ui@example.invalid',fullName:'Khách kiểm tra'}}));
  await page.route('**/api/orders/me?**',r=>r.fulfill({json:{items:[],page:1,size:5,totalCount:0}}));
  await page.route('**/api/admin/orders?**',r=>r.fulfill({json:{items:[],page:1,size:10,totalCount:0}}));
  async function login(){await page.getByLabel('Email',{exact:true}).fill('ui@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('Sample-pass-123');await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();}
  await page.goto(base+'/login');await login();await page.getByRole('alert').filter({hasText:'Email hoặc mật khẩu không đúng.'}).waitFor();
  await page.getByRole('button',{name:'Hiện mật khẩu'}).click();if(await page.locator('#password').getAttribute('type')!=='text')throw Error('Password toggle');
  mode='success';await login();await page.locator('.welcome-poster').waitFor();
  if(await page.evaluate(()=>localStorage.length+sessionStorage.length)!==0)throw Error('Persistent credentials');
  await page.getByRole('button',{name:'Xem thông tin tài khoản'}).click();await page.keyboard.press('Escape');if(await page.locator('#account-popover').count())throw Error('Escape');
  await page.getByRole('button',{name:'Xem thông tin tài khoản'}).click();await page.getByRole('link',{name:'Thông tin cá nhân',exact:true}).click();await page.locator('.profile-card').waitFor();await page.locator('.profile-orders').waitFor();if(await page.locator('.welcome-poster').count())throw Error('Welcome on profile');
  await page.locator('.profile-shell .header').waitFor();
  await page.getByLabel('Tìm bàn phím',{exact:true}).fill('DEMO');await page.getByRole('button',{name:'Tìm kiếm',exact:true}).click();await page.waitForURL('**/catalog?q=DEMO');await page.goBack();await page.locator('.profile-card').waitFor();
  await page.setViewportSize({width:390,height:900});await page.getByRole('button',{name:'Mở menu',exact:true}).click();await page.getByRole('button',{name:'Tất cả sản phẩm',exact:true}).click();await page.locator('#catalog-menu').waitFor();await page.keyboard.press('Escape');await page.setViewportSize({width:1440,height:900});
  await page.getByRole('button',{name:'Chỉnh sửa thông tin',exact:true}).click();
  await page.getByLabel('Họ tên',{exact:true}).fill('Tên chưa lưu');await page.keyboard.press('Escape');
  await page.getByRole('button',{name:'Chỉnh sửa thông tin',exact:true}).click();
  if(await page.getByLabel('Họ tên',{exact:true}).inputValue()!=='Khách kiểm tra')throw Error('Unsaved name persisted');
  await page.getByLabel('Họ tên',{exact:true}).fill('  Hồ sơ mới  ');await page.getByLabel('Email',{exact:true}).fill('new@example.invalid');
  putStatus=409;await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByRole('alert').filter({hasText:'tài khoản khác'}).waitFor();
  if(await page.getByLabel('Họ tên',{exact:true}).inputValue()!=='  Hồ sơ mới  ')throw Error('Lost input');
  putStatus=400;await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByRole('alert').filter({hasText:'chưa hợp lệ'}).waitFor();
  putStatus=200;await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByText('Đã lưu thông tin cá nhân.',{exact:true}).waitFor();
  if(fullName!=='Hồ sơ mới')throw Error('Trim');await page.locator('.profile-identity h2').filter({hasText:'Hồ sơ mới'}).waitFor();
  await page.getByRole('button',{name:'Chỉnh sửa thông tin',exact:true}).click();
  putStatus=0;await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByRole('button',{name:'Tải lại hồ sơ',exact:true}).waitFor();
  if(!await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).isDisabled())throw Error('Uncertain write unlocked');
  const before=writes;await page.getByRole('button',{name:'Tải lại hồ sơ',exact:true}).click();await page.getByLabel('Họ tên',{exact:true}).waitFor();
  await page.waitForFunction(()=>!document.querySelector('#profile-full-name').disabled);if(writes!==before)throw Error('Automatic retry');putStatus=200;
  for(const width of [1440,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Profile overflow');}
  await page.screenshot({path:'web/profile-mobile.png'});await page.setViewportSize({width:1440,height:900});await page.screenshot({path:'web/profile-desktop.png'});
  await page.keyboard.press('Escape');await page.goBack();await page.locator('.welcome-poster').waitFor();await page.getByRole('heading',{name:'Hello, Hồ sơ mới'}).waitFor();await page.goForward();await page.locator('.profile-card').waitFor();
  await page.getByRole('button',{name:'Chỉnh sửa thông tin',exact:true}).click();putStatus=401;await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByRole('button',{name:'Đăng nhập',exact:true}).waitFor();putStatus=200;await login();await page.locator('.profile-card').waitFor();
  await page.reload();await page.getByText('Bạn cần đăng nhập để xem tài khoản.',{exact:false}).waitFor();await login();await page.waitForURL('**/account');await page.locator('.profile-card').waitFor();
  await page.getByRole('button',{name:'Đăng xuất',exact:true}).click();await page.getByRole('link',{name:'Tạo tài khoản',exact:true}).click();
  await page.getByLabel('Họ tên',{exact:true}).fill('Khách kiểm tra');await page.getByLabel('Email',{exact:true}).fill('ui@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('Sample-pass-123');await page.getByLabel('Nhập lại mật khẩu',{exact:true}).fill('Mismatch-123');
  await page.getByRole('button',{name:'Tạo tài khoản',exact:true}).click();await page.getByText('Mật khẩu nhập lại chưa khớp.').waitFor();await page.getByLabel('Nhập lại mật khẩu',{exact:true}).fill('Sample-pass-123');await page.getByRole('button',{name:'Tạo tài khoản',exact:true}).click();await page.getByRole('alert').filter({hasText:'Email đã được sử dụng.'}).waitFor();
  for(const width of [1440,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);}
  registerStatus=201;await page.getByRole('button',{name:'Tạo tài khoản',exact:true}).click();await page.getByText('Tạo tài khoản thành công. Bạn có thể đăng nhập ngay.').waitFor();
  mode='error';await login();await page.getByRole('alert').filter({hasText:'Chưa thể kết nối'}).waitFor();mode='success';meStatus=401;await login();await page.getByRole('button',{name:'Đăng nhập',exact:true}).waitFor();
  meStatus=200;expires=1500;await login();await page.locator('.welcome-poster').waitFor();await page.getByRole('button',{name:'Đăng nhập',exact:true}).waitFor();
  expires=600000;role='Admin';await login();await page.locator('.welcome-poster').waitFor();await page.getByRole('button',{name:'Xem thông tin tài khoản'}).click();if(await page.getByRole('link',{name:'Giỏ hàng của tôi',exact:true}).count())throw Error('Admin cart link');await page.getByRole('link',{name:'Quản lý đơn hàng',exact:true}).click();await page.getByRole('heading',{name:'Quản lý đơn hàng',exact:true}).waitFor();await page.getByText('Chưa có đơn hàng ở trạng thái này.',{exact:true}).waitFor();
  await page.goto(base+'/login?next=https%3A%2F%2Fexample.com');await login();await page.waitForURL('**/welcome');
  await page.goto(base+'/missing-page');await page.getByRole('heading',{name:'Không tìm thấy trang.',exact:true}).waitFor();await page.goto(base+'/admin/products/7/extra');await page.getByRole('heading',{name:'Không tìm thấy trang.',exact:true}).waitFor();
  await page.unrouteAll({behavior:'wait'});await page.goto(base+'/login');
  if(errors.length)throw Error(errors.join(';'));
  return {passed:true,mocked:true,checks:['profile PUT200/400/409/401/network','profile GET/session/trim/Bearer','profile responsive320-1440','401/503','register201/409','welcome/profile/Back','reload and expiry','Admin menu','safe next','404'],errors};
}






async page=>{
  const base='http://127.0.0.1:4173',errors=[];page.on('pageerror',e=>errors.push(e.message));
  let role='Admin',mode='',writes=0,reads=0;
  let product={id:7,sku:'UI-TEST',name:'Bàn phím thử nghiệm',brand:'ShopFlow',categoryId:1,layout:'75%',color:'Đen',switchType:'Linear',connectivity:['Wired'],isHotSwappable:null,description:'',price:1500000,stock:10,isActive:true,rowVersion:42};
  await page.route('**/api/auth/login',r=>r.fulfill({json:{accessToken:'product-ui-only',expiresAt:new Date(Date.now()+600000).toISOString(),user:{id:1,role,email:'ui@example.invalid',fullName:'Quản trị thử nghiệm'}}}));
  await page.route('**/api/categories',r=>r.fulfill({json:[{id:1,name:'75%'}]}));
  await page.route('**/api/admin/products**',r=>{
    if(r.request().headers().authorization!=='Bearer product-ui-only')throw Error('Missing bearer');
    const method=r.request().method();
    if(method!=='GET'){
      writes++;if(mode==='network')return r.abort('failed');if(mode)return r.fulfill({status:409,json:{code:mode}});
      if(method==='DELETE'){product={...product,isActive:false,rowVersion:product.rowVersion+1};return r.fulfill({status:204});}
      const body=r.request().postDataJSON();if(method==='PUT'&&body.rowVersion!==product.rowVersion)throw Error('Wrong version');
      if(method==='POST'&&'rowVersion' in body)throw Error('Create has version');
      if(!Array.isArray(body.connectivity)||body.isHotSwappable!==null)throw Error('Bad config contract');
      product={...product,...body,rowVersion:product.rowVersion+1};return r.fulfill({status:method==='POST'?201:200,json:product});
    }
    reads++;if(mode==='401')return r.fulfill({status:401});
    const url=new URL(r.request().url());return r.fulfill({json:url.pathname==='/api/admin/products'?{items:[product],page:Number(url.searchParams.get('page')),size:10,totalCount:11}:product});
  });
  async function enter(path='/admin/products'){
    await page.goto(base+path);await page.getByRole('link',{name:'Đăng nhập',exact:true}).click();await page.getByLabel('Email',{exact:true}).fill('ui@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('test-only');await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();await page.waitForURL('**'+path);
  }
  await enter();await page.getByRole('link',{name:'Sửa UI-TEST'}).waitFor();await page.getByRole('button',{name:'Tiếp',exact:true}).click();await page.getByText('Trang 2/2',{exact:true}).waitFor();
  await page.getByRole('link',{name:'Thêm sản phẩm',exact:true}).click();await page.getByLabel('Tên sản phẩm',{exact:true}).fill('Bàn phím mới');await page.getByLabel('SKU',{exact:true}).fill('ui-new');await page.getByLabel('Thương hiệu',{exact:true}).fill('ShopFlow');await page.getByLabel('Màu sắc',{exact:true}).fill('Đen');await page.getByLabel('Layout',{exact:true}).selectOption('1');await page.getByLabel('Giá bán (VND)',{exact:true}).fill('1700000');
  await page.getByLabel('Có dây',{exact:true}).uncheck();await page.getByRole('button',{name:'Tạo sản phẩm',exact:true}).click();await page.getByRole('alert').waitFor();if(writes)throw Error('Invalid config posted');await page.getByLabel('Có dây',{exact:true}).check();
  mode='SKU_EXISTS';await page.getByRole('button',{name:'Tạo sản phẩm',exact:true}).click();await page.getByRole('alert').filter({hasText:'SKU này đã tồn tại'}).waitFor();if(await page.getByRole('button',{name:'Tạo sản phẩm',exact:true}).isDisabled())throw Error('SKU error blocked form');
  mode='';await page.getByRole('button',{name:'Tạo sản phẩm',exact:true}).click();await page.waitForURL('**/admin/products/7');await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).waitFor();
  await page.getByLabel('Tồn kho mới',{exact:true}).fill('25');await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByText('Đã lưu thay đổi.',{exact:true}).waitFor();if(product.stock!==25)throw Error('Stock contract');
  for(const width of [1440,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);}
  await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:'.playwright-cli/admin-product-desktop.png',fullPage:true});await page.setViewportSize({width:390,height:844});await page.screenshot({path:'.playwright-cli/admin-product-mobile.png',fullPage:true});
  const before=writes;await page.getByRole('button',{name:'Ngừng bán',exact:true}).click();await page.getByRole('button',{name:'Quay lại',exact:true}).click();if(writes!==before)throw Error('Dialog wrote');await page.getByRole('button',{name:'Ngừng bán',exact:true}).click();await page.getByRole('button',{name:'Xác nhận ngừng bán',exact:true}).click();await page.getByText('Đã ngừng bán sản phẩm.',{exact:true}).waitFor();if(await page.getByLabel('Cho phép bán sản phẩm').isChecked())throw Error('Deactivate failed');
  await page.getByLabel('Cho phép bán sản phẩm').check();await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByText('Đã lưu thay đổi.',{exact:true}).waitFor();
  for(const code of ['CONCURRENCY_CONFLICT','PRODUCT_CONFIGURATION_LOCKED','network']){
    mode=code;await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).click();await page.getByRole('alert').waitFor();if(!await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).isDisabled())throw Error('Stale form enabled');mode='';await page.getByRole('button',{name:'Tải lại bản đã lưu',exact:true}).click();await page.getByRole('button',{name:'Lưu thay đổi',exact:true}).waitFor();
  }
  mode='401';await page.getByRole('button',{name:'Tải lại bản đã lưu',exact:true}).click();await page.getByRole('heading',{name:'Đăng nhập để quản lý sản phẩm'}).waitFor();
  mode='';role='Customer';const readBefore=reads;await enter();await page.getByRole('heading',{name:'Khu vực dành cho quản trị viên'}).waitFor();if(reads!==readBefore)throw Error('Customer API access');
  await page.unroute('**/api/auth/login');await page.unroute('**/api/categories');await page.unroute('**/api/admin/products**');await page.goto(base+'/admin/products');if(errors.length)throw Error(errors.join(';'));
  console.log('PASS products list/paging/create/update/rowVersion/deactivate/activate/conflicts/auth/responsive; writes mocked');
}

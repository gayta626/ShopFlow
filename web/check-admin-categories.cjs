async page=>{
  const base='http://127.0.0.1:4173',errors=[];page.on('pageerror',e=>errors.push(e.message));
  let role='Admin', mode='', writes=[], reads=0, items=[{id:1,name:'75%'},{id:2,name:'TKL'}];
  await page.setViewportSize({width:1440,height:900});
  await page.route('**/api/auth/login',r=>r.fulfill({json:{accessToken:'category-ui-only',expiresAt:new Date(Date.now()+600000).toISOString(),user:{id:1,role,email:'ui@example.invalid',fullName:'Quản trị thử nghiệm'}}}));
  await page.route('**/api/admin/categories**',r=>{
    const method=r.request().method(), id=Number(new URL(r.request().url()).pathname.split('/')[4]);
    if(r.request().headers().authorization!=='Bearer category-ui-only')throw Error('Missing Bearer');
    if(method==='GET'){reads++;return mode==='401'||mode==='403'||mode==='503'?r.fulfill({status:Number(mode)}):r.fulfill({json:items});}
    const body=method==='DELETE'?null:r.request().postDataJSON();writes.push({method,id,body});
    if(mode==='network')return r.abort();
    if(mode==='duplicate')return r.fulfill({status:409,json:method==='POST'?{}:{code:'CATEGORY_EXISTS'}});
    if(mode==='conflict')return r.fulfill({status:409,json:{code:'CONCURRENCY_CONFLICT'}});
    if(mode==='404')return r.fulfill({status:404});
    if(method==='DELETE'){
      if(id===1)return r.fulfill({status:409,json:{code:'CATEGORY_IN_USE'}});
      items=items.filter(c=>c.id!==id);return r.fulfill({status:204});
    }
    if(method==='PUT'&&id===1&&!body.confirmRename)return r.fulfill({status:409,json:{code:'CATEGORY_RENAME_CONFIRMATION_REQUIRED'}});
    const result={id:method==='POST'?3:id,name:body.name};
    items=method==='POST'?[...items,result]:items.map(c=>c.id===id?result:c);
    return r.fulfill({status:method==='POST'?201:200,json:result});
  });
  async function login(){await page.goto(base+'/login?next=%2Fadmin%2Fcategories');await page.getByLabel('Email',{exact:true}).fill('ui@example.invalid');await page.getByLabel('Mật khẩu',{exact:true}).fill('Example-123');await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();await page.waitForURL('**/admin/categories');}
  const add=()=>page.getByRole('button',{name:'Thêm danh mục',exact:true}), save=()=>page.getByRole('button',{name:'Lưu tên',exact:true}), input=()=>page.getByLabel('Tên danh mục',{exact:true});
  await login();await page.getByRole('button',{name:'Đổi tên 75%',exact:true}).waitFor();
  await input().fill('   ');await add().click();await page.getByRole('alert').waitFor();if(writes.length)throw Error('Blank written');
  await input().fill('  65%  ');mode='duplicate';await add().click();await page.getByRole('alert').filter({hasText:'đã tồn tại'}).waitFor();if(await input().inputValue()!=='  65%  ')throw Error('Lost input');
  mode='';await add().click();await page.getByText('Đã thêm danh mục.',{exact:true}).waitFor();if(items[2].name!=='65%')throw Error('Trim');
  await page.getByRole('button',{name:'Đổi tên TKL',exact:true}).click();await input().fill('80%');await save().click();await page.getByRole('button',{name:'Đổi tên 80%',exact:true}).waitFor();if(writes.at(-1).body.confirmRename!==false)throw Error('Implicit confirmation');
  await page.getByRole('button',{name:'Đổi tên 75%',exact:true}).click();await input().fill('Layout 75');await save().click();await page.getByRole('dialog').waitFor();const count=writes.length;await page.getByRole('button',{name:'Quay lại',exact:true}).click();if(writes.length!==count)throw Error('Cancel wrote');
  await save().click();await page.getByRole('dialog').waitFor();await page.getByRole('button',{name:'Xác nhận đổi tên',exact:true}).click();await page.getByRole('button',{name:'Đổi tên Layout 75',exact:true}).waitFor();if(writes.at(-1).body.confirmRename!==true||writes.at(-1).body.name!=='Layout 75')throw Error('Wrong confirmation payload');
  await page.getByRole('button',{name:'Xóa Layout 75',exact:true}).click();await page.getByRole('button',{name:'Xác nhận xóa',exact:true}).click();await page.getByRole('alert').filter({hasText:'còn sản phẩm'}).waitFor();
  await page.getByRole('button',{name:'Xóa 65%',exact:true}).click();await page.keyboard.press('Escape');const beforeDelete=writes.length;if(await page.getByRole('dialog').isVisible())throw Error('Escape');
  await page.getByRole('button',{name:'Xóa 65%',exact:true}).click();await page.getByRole('button',{name:'Xác nhận xóa',exact:true}).click();await page.getByText('Đã xóa danh mục.',{exact:true}).waitFor();if(writes.length!==beforeDelete+1||items.some(c=>c.id===3))throw Error('Delete');
  await page.getByRole('button',{name:'Đổi tên 80%',exact:true}).click();await input().fill('TKL');mode='conflict';await save().click();await page.getByRole('alert').filter({hasText:'nơi khác'}).waitFor();if(!await save().isDisabled())throw Error('Conflict not blocked');
  mode='';await page.getByRole('button',{name:'Tải lại danh sách',exact:true}).click();await page.waitForFunction(()=>!document.querySelector('#category-name').disabled);
  mode='network';await input().fill('New');await add().click();await page.getByRole('alert').filter({hasText:'Chưa xác nhận'}).waitFor();if(!await add().isDisabled())throw Error('Unknown write not blocked');
  mode='';await page.getByRole('button',{name:'Tải lại danh sách',exact:true}).click();await page.waitForFunction(()=>!document.querySelector('#category-name').disabled);
  for(const width of [1440,768,390,320]){await page.setViewportSize({width,height:900});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Overflow '+width);}
  await page.screenshot({path:'web/categories-mobile.png'});await page.setViewportSize({width:1440,height:900});await page.screenshot({path:'web/categories-desktop.png'});
  mode='401';await page.getByRole('button',{name:'Tải lại danh sách',exact:true}).click();await page.getByRole('heading',{name:'Đăng nhập để quản lý danh mục',exact:true}).waitFor();
  mode='';role='Customer';const prev=reads;await login();await page.getByRole('heading',{name:'Khu vực dành cho quản trị viên',exact:true}).waitFor();if(reads!==prev)throw Error('Customer API request');
  if(errors.length)throw Error(errors.join(';'));await page.unrouteAll({behavior:'wait'});
  return {passed:true,mocked:true,writes:writes.length,checks:['list','create201/409/trim','rename/confirmation/cancel','delete204/in-use','conflict/network lock','401/Customer','responsive320-1440']};
}

import { useEffect, useRef, useState } from 'react';
import { useSession, getSession, clearSession } from '../lib/auth';
import { navigate } from '../lib/navigation';
import { money } from '../lib/api';
import '../cart.css';
import '../admin.css';

const empty={sku:'',name:'',brand:'',categoryId:'',color:'',switchType:'Unknown',connectivity:['Wired'],isHotSwappable:null,description:'',price:'',stock:0,isActive:true};
const connections=[['Wired','Có dây'],['Bluetooth','Bluetooth'],['Wireless2_4G','Không dây 2.4 GHz']];
export default function AdminProductsPage(){
  const session=useSession(), route=location.pathname.split('/')[3], creating=route==='new', editing=Boolean(route);
  const [data,setData]=useState(null),[form,setForm]=useState({...empty}),[categories,setCategories]=useState([]),[page,setPage]=useState(1),[retry,setRetry]=useState(0),[error,setError]=useState(''),[notice,setNotice]=useState(''),[busy,setBusy]=useState(false),[blocked,setBlocked]=useState(false),[loading,setLoading]=useState(true);
  const lock=useRef(false),mounted=useRef(false),dialog=useRef(null);
  useEffect(()=>{document.title='Quản lý sản phẩm | ShopFlow';mounted.current=true;return()=>{mounted.current=false;};},[]);
  async function request(path,method='GET',body,signal){
    const response=await fetch(path,{method,signal,headers:{Accept:'application/json',Authorization:`Bearer ${session.accessToken}`,...(body?{'Content-Type':'application/json'}:{})},...(body?{body:JSON.stringify(body)}:{})});
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    if(response.status===401){clearSession();throw new DOMException('Session ended','AbortError');}
    if(!response.ok){
      const detail=await response.json().catch(()=>({}));
      const messages={SKU_EXISTS:'SKU này đã tồn tại. Hãy chọn mã khác hoặc kiểm tra sản phẩm đã tạo.',CATEGORY_NOT_FOUND:'Danh mục không còn tồn tại. Hãy tải lại dữ liệu.',PRODUCT_CONFIGURATION_LOCKED:'Sản phẩm đã có trong đơn hàng: không thể đổi SKU, layout, thương hiệu, màu hoặc cấu hình. Tải lại rồi chỉ sửa tên, mô tả, giá, tồn kho hoặc trạng thái bán.',CONCURRENCY_CONFLICT:'Sản phẩm đã được cập nhật ở nơi khác. Tải lại bản mới trước khi sửa tiếp.'};
      const e=new Error(messages[detail.code]||(response.status===403?'Bạn không có quyền quản lý sản phẩm.':response.status===404?'Không tìm thấy sản phẩm.':response.status===400?'Thông tin chưa hợp lệ. Kiểm tra các trường rồi lưu lại.':method==='GET'?'Chưa tải được dữ liệu. Vui lòng thử lại.':'Chưa xác nhận được kết quả lưu. Kiểm tra danh sách sản phẩm trước khi thử lại.'));
      e.blocked=response.status!==400&&detail.code!=='SKU_EXISTS'||detail.code==='CATEGORY_NOT_FOUND';throw e;
    }
    if(response.status===204)return null;
    const result=await response.json();
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    return result;
  }
  useEffect(()=>{
    setData(null);setError('');setNotice('');setLoading(true);setBlocked(false);
    if(session?.user.role!=='Admin'){setLoading(false);return;}
    if(editing&&!creating&&!/^[1-9]\d*$/.test(route)){setError('Mã sản phẩm không hợp lệ.');setLoading(false);return;}
    const controller=new AbortController();
    (async()=>{
      try{
        if(editing){
          const [options,product]=await Promise.all([request('/api/categories','GET',undefined,controller.signal),creating?Promise.resolve({...empty}):request(`/api/admin/products/${route}`,'GET',undefined,controller.signal)]);
          if(controller.signal.aborted)return;
          if(!Array.isArray(options)||!Array.isArray(product.connectivity)||(!creating&&!Number.isInteger(product.rowVersion)))throw new Error('Dữ liệu sản phẩm chưa đầy đủ. Hãy tải lại.');
          setCategories(options);setForm(product);setData(product);
        }else{const result=await request(`/api/admin/products?page=${page}&size=10`,'GET',undefined,controller.signal);if(!controller.signal.aborted)setData(result);}
      }catch(e){if(e.name!=='AbortError')setError(e.message==='Failed to fetch'?'Không kết nối được cửa hàng. Vui lòng thử lại.':e.message);}
      finally{if(!controller.signal.aborted)setLoading(false);}
    })();return()=>controller.abort();
  },[session,route,page,retry]);
  const change=(key,value)=>setForm(old=>({...old,[key]:value}));
  async function save(event){
    event.preventDefault();if(lock.current||blocked)return;
    const body={categoryId:Number(form.categoryId),sku:form.sku.trim().toUpperCase(),name:form.name.trim(),brand:form.brand.trim(),color:form.color.trim(),switchType:form.switchType,connectivity:form.connectivity,isHotSwappable:form.isHotSwappable,description:form.description.trim(),price:Number(form.price),stock:Number(form.stock),isActive:form.isActive,...(!creating?{rowVersion:form.rowVersion}:{})};
    if(!body.name||!body.brand||!body.color||!body.categoryId||!body.connectivity.length||!/^[A-Z0-9-]{3,50}$/.test(body.sku)||!Number.isInteger(body.price)||body.price<1||body.price>1000000000||!Number.isInteger(body.stock)||body.stock<0||body.stock>1000000){setError('Kiểm tra thông tin bắt buộc, SKU, giá nguyên VND và chọn ít nhất một kết nối.');return;}
    lock.current=true;setBusy(true);setError('');setNotice('');
    try{
      const result=await request(`/api/admin/products${creating?'':`/${route}`}`,creating?'POST':'PUT',body);
      if(!result?.id||!Number.isInteger(result.rowVersion))throw new Error('Chưa xác nhận được bản lưu. Hãy kiểm tra danh sách sản phẩm.');
      if(mounted.current){if(creating)navigate(`/admin/products/${result.id}`,true);else{setData(result);setForm(result);setNotice('Đã lưu thay đổi.');}}
    }catch(e){if(mounted.current&&getSession()===session&&e.name!=='AbortError'){setError(e.message==='Failed to fetch'?'Chưa xác nhận được kết quả lưu. Kiểm tra danh sách sản phẩm trước khi thử lại.':e.message);setBlocked(e.blocked!==false);}}
    finally{lock.current=false;if(mounted.current)setBusy(false);}
  }
  async function deactivate(){
    if(lock.current||blocked)return;lock.current=true;setBusy(true);dialog.current.close();setError('');
    try{await request(`/api/admin/products/${route}`,'DELETE');const result=await request(`/api/admin/products/${route}`);if(mounted.current){setData(result);setForm(result);setNotice('Đã ngừng bán sản phẩm.');}}
    catch(e){if(mounted.current&&getSession()===session&&e.name!=='AbortError'){setBlocked(true);setError('Chưa xác nhận được trạng thái sản phẩm. Tải lại để kiểm tra trước khi thao tác tiếp.');}}
    finally{lock.current=false;if(mounted.current)setBusy(false);}
  }
  const field=(key,label,props={})=><label key={key} className="product-field" htmlFor={`product-${key}`}>{label}<input id={`product-${key}`} value={form[key]} onChange={e=>change(key,e.target.value)} {...props}/></label>;
  return <><header className="cart-header"><a className="brand" href="/" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt=""/><span>SHOP<span>FLOW</span><i/></span></a><nav><a href="/admin/products">Sản phẩm</a><a href="/admin/orders">Đơn hàng</a><a href="/admin/categories">Danh mục</a><a href="/account">Tài khoản</a></nav></header><main className="cart-page admin-orders" tabIndex="-1"><span className="cart-eyebrow">SHOPFLOW / QUẢN TRỊ</span><h1>{creating?'Thêm sản phẩm':editing?'Chỉnh sửa sản phẩm':'Quản lý sản phẩm'}</h1>
    {!session?<section className="cart-feedback"><h2>Đăng nhập để quản lý sản phẩm</h2><a className="cart-primary" href={`/login?next=${encodeURIComponent(location.pathname)}`}>Đăng nhập</a></section>:session.user.role!=='Admin'?<section className="cart-feedback"><h2>Khu vực dành cho quản trị viên</h2><a href="/account">Về tài khoản</a></section>:<>
      <div className="admin-toolbar">{editing?<a href="/admin/products">← Danh sách sản phẩm</a>:<a className="cart-primary" href="/admin/products/new">Thêm sản phẩm</a>}<button disabled={busy} onClick={()=>setRetry(n=>n+1)}>{editing?'Tải lại bản đã lưu':'Tải lại'}</button></div>
      {error&&<div className="cart-alert" role="alert">{error}{blocked&&<p><a href="/admin/products">Kiểm tra danh sách sản phẩm</a></p>}</div>}{notice&&<p className="cart-success" role="status">{notice}</p>}
      {loading?<p role="status">Đang tải sản phẩm…</p>:data&&(!editing?<section className="admin-list"><div className="admin-list-heading"><h2>Tất cả sản phẩm</h2><span>{data.totalCount} sản phẩm</span></div>{!data.items.length?<p className="cart-feedback">Chưa có sản phẩm nào.</p>:<div className="admin-table"><table><thead><tr><th>Sản phẩm / SKU</th><th>Layout</th><th>Giá</th><th>Tồn kho</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>{data.items.map(product=><tr key={product.id}><td><strong>{product.name}</strong><small className="product-sku">{product.sku}</small></td><td>{product.layout}</td><td>{money(product.price)}</td><td>{product.stock}</td><td><span className={`admin-status ${product.isActive?'Paid':'Cancelled'}`}>{product.isActive?'Đang bán':'Ngừng bán'}</span></td><td><a href={`/admin/products/${product.id}`} aria-label={`Sửa ${product.sku}`}>Chỉnh sửa ↗</a></td></tr>)}</tbody></table></div>}<nav className="admin-pagination" aria-label="Phân trang"><span>Trang {data.page}/{Math.max(1,Math.ceil(data.totalCount/data.size))}</span><div><button disabled={page<=1} onClick={()=>setPage(n=>n-1)}>Trước</button><button disabled={page*data.size>=data.totalCount} onClick={()=>setPage(n=>n+1)}>Tiếp</button></div></nav></section>:<form onSubmit={save} className="product-editor" aria-busy={busy}><fieldset disabled={busy||blocked} className="product-form-fields"><section className="product-form-card"><h2>Thông tin sản phẩm</h2><div className="product-form-grid">{field('name','Tên sản phẩm',{required:true,maxLength:200})}{field('sku','SKU',{required:true,minLength:3,maxLength:50,pattern:'[A-Za-z0-9-]{3,50}'})}{field('brand','Thương hiệu',{required:true,maxLength:100})}{field('color','Màu sắc',{required:true,maxLength:50})}<label className="product-field" htmlFor="product-category">Layout<select aria-label="Layout" id="product-category" required value={form.categoryId} onChange={e=>change('categoryId',e.target.value)}><option value="">Chọn layout</option>{categories.map(c=><option key={c.id} value={c.id}>{c.name}</option>)}</select></label><label className="product-field" htmlFor="product-switch">Loại switch<select aria-label="Loại switch" id="product-switch" value={form.switchType} onChange={e=>change('switchType',e.target.value)}>{['Unknown','Linear','Tactile','Clicky'].map(value=><option key={value} value={value}>{value==='Unknown'?'Chưa xác định':value}</option>)}</select></label></div><fieldset className="product-connections"><legend>Kết nối · chọn ít nhất một</legend>{connections.map(([value,label])=><label key={value}><input type="checkbox" checked={form.connectivity.includes(value)} onChange={e=>change('connectivity',e.target.checked?[...form.connectivity,value]:form.connectivity.filter(item=>item!==value))}/>{label}</label>)}</fieldset><label className="product-field" htmlFor="product-hot">Thay switch nóng<select aria-label="Thay switch nóng" id="product-hot" value={form.isHotSwappable===null?'unknown':String(form.isHotSwappable)} onChange={e=>change('isHotSwappable',e.target.value==='unknown'?null:e.target.value==='true')}><option value="unknown">Chưa có thông tin</option><option value="true">Có hỗ trợ</option><option value="false">Không hỗ trợ</option></select></label><label className="product-field" htmlFor="product-description">Mô tả<textarea id="product-description" rows="5" maxLength="2000" value={form.description} onChange={e=>change('description',e.target.value)}/></label>{!creating&&<p className="admin-note">Sản phẩm đã có trong đơn hàng chỉ được sửa tên, mô tả, giá, tồn kho và trạng thái bán.</p>}</section><section className="product-form-card"><h2>Giá & tồn kho</h2>{field('price','Giá bán (VND)',{type:'number',min:1,max:1000000000,step:1,required:true})}{field('stock','Tồn kho mới',{type:'number',min:0,max:1000000,step:1,required:true})}<p className="admin-note">Số lượng nhập vào thay thế tồn kho hiện tại.</p><label className="product-active"><input type="checkbox" checked={form.isActive} onChange={e=>change('isActive',e.target.checked)}/>Cho phép bán sản phẩm</label><button className="cart-primary" type="submit" disabled={!categories.length}>{busy?'Đang lưu…':creating?'Tạo sản phẩm':'Lưu thay đổi'}</button>{!categories.length&&<p className="cart-alert">Cần có danh mục trước khi tạo hoặc sửa sản phẩm.</p>}<p className="admin-note">Ảnh sản phẩm sẽ được bổ sung khi chức năng quản lý ảnh sẵn sàng.</p></section></fieldset></form>)}
      {editing&&!creating&&data?.isActive&&<div className="product-stop"><div><h2>Ngừng bán sản phẩm</h2><p>Sản phẩm sẽ ẩn khỏi cửa hàng. Dữ liệu đơn hàng được giữ lại.</p></div><button className="admin-cancel" disabled={busy||blocked} onClick={()=>dialog.current.showModal()}>Ngừng bán</button></div>}
    </>}
    <dialog className="cart-confirm" ref={dialog} aria-labelledby="stop-title"><h2 id="stop-title">Ngừng bán sản phẩm?</h2><p>Sản phẩm sẽ không còn được đặt mua. Các thay đổi chưa lưu trong biểu mẫu sẽ không được áp dụng.</p><div><button autoFocus onClick={()=>dialog.current.close()}>Quay lại</button><button className="cart-primary" disabled={busy} onClick={deactivate}>Xác nhận ngừng bán</button></div></dialog>
  </main></>;
}



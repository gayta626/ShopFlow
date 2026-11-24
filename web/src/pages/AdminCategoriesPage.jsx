import { useEffect, useRef, useState } from 'react';
import { useSession, getSession, clearSession } from '../lib/auth';
import '../cart.css';
import '../admin.css';
import '../categories.css';

export default function AdminCategoriesPage() {
  const session = useSession();
  const [items,setItems] = useState([]), [loading,setLoading] = useState(true);
  const [busy,setBusy] = useState(false), [blocked,setBlocked] = useState(false);
  const [name,setName] = useState(''), [editing,setEditing] = useState(null);
  const [error,setError] = useState(''), [notice,setNotice] = useState('');
  const [pending,setPending] = useState(null), [retry,setRetry] = useState(0);
  const lock = useRef(false), mounted = useRef(false), dialog = useRef(null), input = useRef(null);
  useEffect(() => { document.title='Quản lý danh mục | ShopFlow'; mounted.current=true; return()=>{mounted.current=false;}; }, []);
  async function request(method='GET', id, body, signal) {
    const response = await fetch(`/api/admin/categories${id ? `/${id}` : ''}`, {
      method, signal, headers:{Accept:'application/json',Authorization:`Bearer ${session.accessToken}`,...(body?{'Content-Type':'application/json'}:{})},
      ...(body?{body:JSON.stringify(body)}:{})
    });
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    if(response.status===401){clearSession();throw new DOMException('Session ended','AbortError');}
    if(!response.ok){
      const detail=await response.json().catch(()=>({}));
      if(getSession()!==session)throw new DOMException('Session changed','AbortError');
      const messages={CATEGORY_EXISTS:'Tên danh mục đã tồn tại. Hãy chọn tên khác.',CATEGORY_IN_USE:'Không thể xóa danh mục còn sản phẩm, kể cả sản phẩm đã ngừng bán.',CONCURRENCY_CONFLICT:'Danh mục vừa thay đổi ở nơi khác. Tải lại danh sách trước khi sửa tiếp.'};
      const e=new Error(messages[detail.code] || (response.status===409&&method==='POST'?'Tên danh mục đã tồn tại. Hãy chọn tên khác.':response.status===400?'Tên danh mục phải có từ 1–100 ký tự sau khi bỏ khoảng trắng.':response.status===403?'Bạn không có quyền quản lý danh mục.':response.status===404?'Danh mục không còn tồn tại. Tải lại danh sách.':method==='GET'?'Chưa tải được danh mục. Vui lòng thử lại.':'Chưa xác nhận được kết quả lưu. Tải lại danh sách để kiểm tra.'));
      e.code=detail.code; e.status=response.status; throw e;
    }
    if(response.status===204)return null;
    const result=await response.json();
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    return result;
  }
  useEffect(()=>{
    if(session?.user.role!=='Admin'){setLoading(false);return;}
    const controller=new AbortController(); setLoading(true);setError('');
    request('GET',undefined,undefined,controller.signal).then(result=>{
      if(controller.signal.aborted)return;
      if(!Array.isArray(result)||result.some(c=>!Number.isInteger(c.id)||typeof c.name!=='string'))throw new Error('Dữ liệu danh mục chưa hợp lệ. Vui lòng tải lại.');
      setItems(result);setBlocked(false);setEditing(null);setName('');setPending(null);dialog.current?.close();
    }).catch(e=>{if(e.name!=='AbortError'&&!controller.signal.aborted){setError(e.status?e.message:'Chưa tải được danh mục. Vui lòng thử lại.');setBlocked(true);}}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return()=>controller.abort();
  },[session,retry]);
  useEffect(()=>{if(pending)dialog.current?.showModal();},[pending]);
  const valid=c=>c&&Number.isInteger(c.id)&&typeof c.name==='string';
  async function mutate(method,id,body) {
    if(lock.current||blocked||loading||session?.user.role!=='Admin')return;
    lock.current=true;setBusy(true);setError('');setNotice('');
    try {
      const result=await request(method,id,body);
      if(!mounted.current)return;
      if(method==='DELETE') {
        setItems(old=>old.filter(c=>c.id!==id));setNotice('Đã xóa danh mục.');
        if(editing?.id===id){setEditing(null);setName('');}
      } else {
        if(!valid(result)||(method==='PUT'&&result.id!==id))throw new Error('Invalid response');
        setItems(old=>method==='POST'?[...old.filter(c=>c.id!==result.id),result]:old.map(c=>c.id===id?result:c));
        setEditing(null);setName('');setNotice(method==='POST'?'Đã thêm danh mục.':'Đã đổi tên danh mục.');
      }
      setPending(null);dialog.current?.close();
    } catch(e) {
      if(!mounted.current||getSession()!==session||e.name==='AbortError')return;
      if(e.code==='CATEGORY_RENAME_CONFIRMATION_REQUIRED'&&method==='PUT'&&!body.confirmRename){
        setPending({type:'rename',id,name:body.name,oldName:editing?.name});
      } else {
        setPending(null);dialog.current?.close();
        setError(e.status?e.message:'Chưa xác nhận được kết quả lưu. Tải lại danh sách để kiểm tra trước khi thao tác tiếp.');
        const recoverable=e.status===400||e.code==='CATEGORY_EXISTS'||e.code==='CATEGORY_IN_USE'||(method==='POST'&&e.status===409);
        setBlocked(!recoverable);
      }
    } finally {lock.current=false;if(mounted.current)setBusy(false);}
  }
  function save(event){
    event.preventDefault();const trimmed=name.trim();
    if(!trimmed||trimmed.length>100){setError('Tên danh mục phải có từ 1–100 ký tự sau khi bỏ khoảng trắng.');return;}
    mutate(editing?'PUT':'POST',editing?.id,{name:trimmed,...(editing?{confirmRename:false}:{})});
  }
  function close(){if(lock.current)return;dialog.current.close();setPending(null);}
  const disabled=busy||blocked||loading||Boolean(pending);
  return <>
    <header className="cart-header"><a className="brand" href="/" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt=""/><span>SHOP<span>FLOW</span><i/></span></a><nav aria-label="Quản trị"><a href="/admin/categories" aria-current="page">Danh mục</a><a href="/admin/products">Sản phẩm</a><a href="/admin/orders">Đơn hàng</a><a href="/account">Tài khoản</a></nav></header>
    <main className="cart-page admin-orders categories-page" id="main" tabIndex="-1"><span className="cart-eyebrow">SHOPFLOW / QUẢN TRỊ</span><h1>Quản lý danh mục</h1>
      {!session?<section className="cart-feedback"><h2>Đăng nhập để quản lý danh mục</h2><a className="cart-primary" href="/login?next=%2Fadmin%2Fcategories">Đăng nhập</a></section>:session.user.role!=='Admin'?<section className="cart-feedback"><h2>Khu vực dành cho quản trị viên</h2><a href="/account">Về tài khoản</a></section>:<>
        <div className="admin-toolbar"><p className="admin-note">Danh mục dùng để nhóm bàn phím theo layout.</p><button disabled={busy||loading||Boolean(pending)} onClick={()=>{setNotice('');setRetry(n=>n+1);}}>Tải lại danh sách</button></div>
        {error&&<p className="cart-alert" role="alert">{error}</p>}{notice&&<p className="cart-success" role="status">{notice}</p>}
        <div className="category-layout">
          <section className="product-form-card"><h2>{editing?'Đổi tên danh mục':'Thêm danh mục'}</h2><form onSubmit={save} aria-busy={busy}><label className="product-field" htmlFor="category-name">Tên danh mục<input ref={input} id="category-name" value={name} onChange={e=>setName(e.target.value)} required maxLength="100" disabled={disabled}/></label><p className="admin-note">Từ 1–100 ký tự. Tên không được trùng danh mục khác.</p><div className="category-actions"><button className="cart-primary" disabled={disabled} type="submit">{busy?'Đang xử lý…':editing?'Lưu tên':'Thêm danh mục'}</button>{editing&&<button type="button" disabled={disabled} onClick={()=>{setEditing(null);setName('');setError('');}}>Hủy chỉnh sửa</button>}</div></form></section>
          <section className="admin-list"><div className="admin-list-heading"><h2>Danh mục hiện có</h2><span>{items.length} danh mục</span></div>{loading?<p role="status">Đang tải danh mục…</p>:!items.length?<p className="admin-note">{blocked?'Chưa có dữ liệu để hiển thị.':'Chưa có danh mục nào. Thêm danh mục đầu tiên bên cạnh.'}</p>:<ul className="category-list">{items.map(c=><li key={c.id}><div><strong>{c.name}</strong><small>Mã #{c.id}</small></div><div className="category-actions"><button disabled={disabled} aria-label={`Đổi tên ${c.name}`} onClick={()=>{setEditing(c);setName(c.name);setError('');setNotice('');input.current?.focus();}}>Đổi tên</button><button className="admin-cancel" disabled={disabled} aria-label={`Xóa ${c.name}`} onClick={()=>setPending({type:'delete',id:c.id,name:c.name})}>Xóa</button></div></li>)}</ul>}<p className="admin-note">Chỉ xóa được danh mục không còn sản phẩm.</p></section>
        </div>
        <dialog className="cart-confirm" ref={dialog} aria-labelledby="category-confirm-title" onCancel={e=>{if(lock.current)e.preventDefault();else setPending(null);}}><h2 id="category-confirm-title">{pending?.type==='rename'?'Xác nhận đổi tên danh mục':'Xóa danh mục?'}</h2><p>{pending?.type==='rename'?`Danh mục “${pending.oldName}” đang có sản phẩm. Đổi thành “${pending.name}” sẽ thay đổi tên danh mục hiển thị của tất cả sản phẩm thuộc danh mục này, kể cả sản phẩm ngừng bán.`:`Bạn muốn xóa danh mục “${pending?.name}”? Thao tác chỉ thành công nếu danh mục không còn sản phẩm.`}</p><div><button autoFocus disabled={busy} onClick={close}>Quay lại</button><button className="cart-primary" disabled={busy} onClick={()=>pending?.type==='rename'?mutate('PUT',pending.id,{name:pending.name,confirmRename:true}):mutate('DELETE',pending.id)}>{busy?'Đang xử lý…':pending?.type==='rename'?'Xác nhận đổi tên':'Xác nhận xóa'}</button></div></dialog>
      </>}
    </main>
  </>;
}


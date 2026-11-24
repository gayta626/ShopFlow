import { useEffect, useRef, useState } from 'react';
import { clearSession, useSession } from '../lib/auth';
import { navigate } from '../lib/navigation';

export default function AccountMenu() {
  const session = useSession(), [open,setOpen] = useState(false);
  const root = useRef(null), button = useRef(null);
  useEffect(() => {
    const outside = event => { if (!root.current?.contains(event.target)) setOpen(false); };
    const escape = event => { if (event.key === 'Escape' && open) {setOpen(false);button.current?.focus();} };
    document.addEventListener('pointerdown',outside);document.addEventListener('keydown',escape);
    return ()=>{document.removeEventListener('pointerdown',outside);document.removeEventListener('keydown',escape);};
  },[open]);
  if(!session)return <a href="/login">Đăng nhập</a>;
  return <div className="account-menu" ref={root} onBlur={event=>{if(!event.currentTarget.contains(event.relatedTarget))setOpen(false);}}><button ref={button} className="account-avatar" aria-label="Xem thông tin tài khoản" aria-expanded={open} aria-controls="account-popover" onClick={()=>setOpen(!open)}>{session.user.fullName.trim().slice(0,1).toUpperCase()}</button>{open&&<section className="account-popover" id="account-popover" aria-label="Thông tin tài khoản"><strong>{session.user.fullName}</strong><p>{session.user.email}</p><span>{session.user.role==='Admin'?'Quản trị viên':'Khách hàng'}</span><a href="/account">Thông tin cá nhân</a>{session.user.role==='Admin'?<><a href="/admin/orders">Quản lý đơn hàng</a><a href="/admin/products">Quản lý sản phẩm</a><a href="/admin/categories">Quản lý danh mục</a></>:<a href="/cart">Giỏ hàng của tôi</a>}<button onClick={()=>{clearSession();navigate('/login',true);}}>Đăng xuất</button></section>}</div>;
}




import { useEffect, useRef, useState } from 'react';
import { useSession, getSession, clearSession } from '../lib/auth';
import { money } from '../lib/api';
import '../cart.css';
import '../checkout.css';
import '../admin.css';

const statuses={Pending:'Chờ thanh toán',Paid:'Đã thanh toán',Shipped:'Đang giao',Completed:'Hoàn tất',Cancelled:'Đã hủy'};
const date=value=>new Intl.DateTimeFormat('vi-VN',{dateStyle:'short',timeStyle:'short',timeZone:'Asia/Ho_Chi_Minh'}).format(new Date(value));
export default function AdminOrdersPage(){
  const session=useSession(), id=location.pathname.split('/')[3];
  const [data,setData]=useState(null), [filter,setFilter]=useState(''), [page,setPage]=useState(1), [retry,setRetry]=useState(0), [error,setError]=useState(''), [busy,setBusy]=useState(false), [blocked,setBlocked]=useState(false), [target,setTarget]=useState(''), [notice,setNotice]=useState('');
  const dialog=useRef(null), lock=useRef(false), mounted=useRef(false);
  useEffect(()=>{mounted.current=true;document.title='Quản lý đơn hàng | ShopFlow';return()=>{mounted.current=false;};},[]);
  async function request(path,body,signal){
    const response=await fetch(`/api/admin/orders${path}`,{method:body?'PUT':'GET',signal,headers:{Accept:'application/json',Authorization:`Bearer ${session.accessToken}`,...(body?{'Content-Type':'application/json'}:{})},...(body?{body:JSON.stringify(body)}:{})});
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    if(response.status===401){clearSession();throw new DOMException('Session ended','AbortError');}
    if(!response.ok)throw new Error(response.status===403?'Bạn không có quyền quản lý đơn hàng.':response.status===404?'Không tìm thấy đơn hàng.':response.status===409?'Trạng thái đơn đã thay đổi hoặc thao tác không còn hợp lệ. Tải lại để kiểm tra.':body?'Chưa xác nhận được kết quả cập nhật. Tải lại đơn trước khi thao tác tiếp.':'Chưa tải được đơn hàng. Vui lòng thử lại.');
    const result=await response.json();
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    if(!Array.isArray(result.items))throw new Error('Phản hồi chưa đầy đủ. Vui lòng tải lại.');
    return result;
  }
  useEffect(()=>{
    setData(null);setError('');setNotice('');setBlocked(false);
    if(session?.user.role!=='Admin')return;
    if(id&&!/^[1-9]\d*$/.test(id)){setError('Mã đơn hàng không hợp lệ.');return;}
    const controller=new AbortController();
    request(id?`/${id}`:`?${new URLSearchParams({page,size:10,...(filter?{status:filter}:{})})}`,null,controller.signal).then(result=>{if(!controller.signal.aborted)setData(result);}).catch(e=>{if(e.name!=='AbortError')setError(e.message==='Failed to fetch'?'Không kết nối được cửa hàng. Vui lòng thử lại.':e.message);});
    return()=>controller.abort();
  },[session,id,page,filter,retry]);
  async function update(){
    if(lock.current||blocked||!target)return;
    lock.current=true;setBusy(true);setError('');dialog.current.close();
    try{const result=await request(`/${id}/status`,{status:target});if(mounted.current){setData(result);setNotice(`Đã cập nhật: ${statuses[result.status]||result.status}.`);}}
    catch(e){if(mounted.current&&getSession()===session&&e.name!=='AbortError'){setBlocked(true);setError(e.message==='Failed to fetch'?'Chưa xác nhận được kết quả. Tải lại đơn trước khi thao tác tiếp.':e.message);}}
    finally{lock.current=false;if(mounted.current)setBusy(false);}
  }
  const actions=data?.status==='Pending'?['Paid','Cancelled']:data?.status==='Paid'?['Shipped','Cancelled']:[];
  function choose(value){setTarget(value);dialog.current.showModal();}
  return <><header className="cart-header"><a className="brand" href="/" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt=""/><span>SHOP<span>FLOW</span><i/></span></a><nav><a href="/admin/products">Sản phẩm</a><a href="/admin/orders">Quản lý đơn hàng</a><a href="/admin/categories">Danh mục</a><a href="/account">Tài khoản</a></nav></header><main className="cart-page admin-orders" tabIndex="-1"><span className="cart-eyebrow">SHOPFLOW / QUẢN TRỊ</span><h1>{id?`Đơn hàng #${id}`:'Quản lý đơn hàng'}</h1>
    {!session?<section className="cart-feedback"><h2>Đăng nhập để quản lý đơn hàng</h2><a className="cart-primary" href={`/login?next=${encodeURIComponent(location.pathname)}`}>Đăng nhập</a></section>:session.user.role!=='Admin'?<section className="cart-feedback"><h2>Khu vực dành cho quản trị viên</h2><a href="/account">Về tài khoản</a></section>:<>
      <div className="admin-toolbar">{id?<a href="/admin/orders">← Danh sách đơn hàng</a>:<div className="admin-filter"><label htmlFor="admin-status-filter">Trạng thái</label><select id="admin-status-filter" value={filter} onChange={e=>{setFilter(e.target.value);setPage(1);}}><option value="">Tất cả trạng thái</option>{Object.entries(statuses).map(([value,label])=><option key={value} value={value}>{label}</option>)}</select></div>}<button disabled={busy} onClick={()=>setRetry(n=>n+1)}>Tải lại</button></div>
      {error&&<p className="cart-alert" role="alert">{error}</p>}{notice&&<p className="cart-success" role="status">{notice}</p>}
      {!data&&!error?<p role="status">Đang tải đơn hàng…</p>:data&&(!id?<section className="admin-list"><div className="admin-list-heading"><h2>{filter?statuses[filter]:'Tất cả đơn hàng'}</h2><span>{data.totalCount} đơn · Mới nhất trước</span></div>{!data.items.length?<p className="cart-feedback">Chưa có đơn hàng ở trạng thái này.</p>:<div className="admin-table"><table><thead><tr><th>Mã đơn</th><th>Ngày đặt</th><th>Trạng thái</th><th>Tổng tiền</th><th>Chi tiết</th></tr></thead><tbody>{data.items.map(order=><tr key={order.id}><td><a href={`/admin/orders/${order.id}`}>#{order.id}</a></td><td>{date(order.createdAt)}</td><td><span className={`admin-status ${order.status}`}>{statuses[order.status]||order.status}</span></td><td>{money(order.totalAmount)}</td><td><a href={`/admin/orders/${order.id}`} aria-label={`Xem đơn #${order.id}`}>Xem đơn ↗</a></td></tr>)}</tbody></table></div>}<nav className="admin-pagination" aria-label="Phân trang"><span>Trang {data.page}/{Math.max(1,Math.ceil(data.totalCount/data.size))}</span><div><button disabled={page<=1} onClick={()=>setPage(n=>n-1)}>Trước</button><button disabled={page*data.size>=data.totalCount} onClick={()=>setPage(n=>n+1)}>Tiếp</button></div></nav></section>:<div className="admin-detail"><section><div className="admin-list-heading"><h2>Sản phẩm trong đơn</h2><span className={`admin-status ${data.status}`}>{statuses[data.status]||data.status}</span></div><ul className="checkout-lines">{data.items.map(item=><li key={item.productId}><div><strong>{item.productName}</strong><span>{item.sku}</span><p>{item.quantity} × {money(item.unitPrice)}</p></div><b>{money(item.lineTotal)}</b></li>)}</ul><div className="checkout-total"><span>Tổng tiền</span><strong>{money(data.totalAmount)}</strong></div></section><aside><h2>Thông tin đơn hàng</h2><dl><dt>Ngày đặt</dt><dd>{date(data.createdAt)}</dd><dt>Địa chỉ nhận hàng</dt><dd>{data.shippingAddress}</dd></dl>{actions.length>0?<div className="admin-actions"><h3>Cập nhật trạng thái</h3>{actions.map(value=><button className={value==='Cancelled'?'admin-cancel':'cart-primary'} key={value} disabled={busy||blocked} onClick={()=>choose(value)}>{value==='Paid'?'Xác nhận đã thanh toán':value==='Shipped'?'Bắt đầu giao hàng':'Hủy đơn hàng'}</button>)}{data.status==='Pending'&&<p>Chỉ xác nhận khi đã kiểm tra khoản thanh toán. Nút này không thu tiền qua thẻ.</p>}</div>:<p className="admin-note">{data.status==='Shipped'?'Khách hàng sẽ xác nhận hoàn tất khi đã nhận hàng.':'Không có thao tác trạng thái tiếp theo.'}</p>}</aside></div>)}
    </>}
    <dialog className="cart-confirm" ref={dialog} aria-labelledby="admin-confirm-title"><h2 id="admin-confirm-title">{target==='Cancelled'?'Hủy đơn hàng?':target==='Paid'?'Xác nhận đã thanh toán?':'Bắt đầu giao hàng?'}</h2><p>{target==='Cancelled'?'Đơn sẽ bị hủy và sản phẩm được hoàn kho. Thao tác này không tự hoàn tiền cho khách.':target==='Paid'?'Xác nhận bạn đã kiểm tra và nhận đủ tiền của đơn hàng.':'Chỉ cập nhật khi đơn đã sẵn sàng và được bàn giao để vận chuyển.'}</p><div><button autoFocus onClick={()=>dialog.current.close()}>Quay lại</button><button className="cart-primary" onClick={update} disabled={busy}>Xác nhận</button></div></dialog>
  </main></>;
}




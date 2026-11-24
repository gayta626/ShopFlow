import { useEffect, useRef, useState } from 'react';
import { clearSession, getSession, useSession } from '../lib/auth';
import { money } from '../lib/api';
import '../cart.css';
import '../checkout.css';
import '../order.css';

const stages = [['Pending','Chờ thanh toán'],['Paid','Đã thanh toán'],['Shipped','Đang giao'],['Completed','Hoàn tất']];
const descriptions = {Pending:'Đơn đã được ghi nhận và đang chờ thanh toán.',Paid:'Đơn đã thanh toán, đang chờ được giao.',Shipped:'Đơn đang trên đường đến bạn. Chỉ xác nhận sau khi đã nhận đủ hàng.',Completed:'Bạn đã xác nhận nhận hàng. Cảm ơn bạn đã mua sắm tại ShopFlow.',Cancelled:'Đơn hàng này đã được hủy.'};

export default function OrderPage() {
  const session=useSession(), id=location.pathname.split('/')[2];
  const [order,setOrder]=useState(null), [error,setError]=useState(''), [loading,setLoading]=useState(true), [busy,setBusy]=useState(false), [stale,setStale]=useState(false), [retry,setRetry]=useState(0), [notice,setNotice]=useState('');
  const dialog=useRef(null), lock=useRef(false), mounted=useRef(false);
  useEffect(()=>{document.title=`Đơn #${id} | ShopFlow`;mounted.current=true;return()=>{mounted.current=false;};},[id]);
  async function request(action,signal){
    const response=await fetch(`/api/orders/${id}${action?`/${action}`:''}`,{method:action?'POST':'GET',signal,headers:{Accept:'application/json',Authorization:`Bearer ${session.accessToken}`}});
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    if(response.status===401){clearSession();throw new DOMException('Session ended','AbortError');}
    if(!response.ok){const problem=new Error(response.status===404?'Không tìm thấy đơn hàng trong tài khoản của bạn.':response.status===403?'Tài khoản này không có quyền truy cập đơn hàng.':response.status===409?'Trạng thái đơn vừa thay đổi hoặc không còn cho phép thao tác này. Hãy tải lại đơn.':action?'Chưa xác nhận được kết quả. Hãy tải lại đơn để kiểm tra trạng thái trước khi thao tác tiếp.':'Chưa tải được đơn hàng. Vui lòng thử lại.');throw problem;}
    const result=await response.json();
    if(getSession()!==session)throw new DOMException('Session changed','AbortError');
    if(!result.id || !Array.isArray(result.items))throw new Error('Phản hồi đơn hàng chưa đầy đủ. Hãy tải lại đơn để kiểm tra.');
    return result;
  }
  useEffect(()=>{
    setOrder(null);setError('');setLoading(true);setNotice('');
    if(!session || session.user.role!=='Customer'){setLoading(false);return;}
    if(!/^[1-9]\d*$/.test(id)){setError('Mã đơn hàng không hợp lệ.');setLoading(false);return;}
    const controller=new AbortController();
    request('',controller.signal).then(result=>{if(!controller.signal.aborted){setOrder(result);setStale(false);}}).catch(e=>{if(e.name!=='AbortError')setError(e.message==='Failed to fetch'?'Chưa kết nối được. Vui lòng tải lại đơn.':e.message);}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return()=>controller.abort();
  },[session,id,retry]);
  const action=order?.status==='Pending'?'cancel':order?.status==='Shipped'?'complete':null;
  async function confirm(){
    if(lock.current || stale || !action)return;
    lock.current=true;setBusy(true);setError('');dialog.current.close();
    try{const result=await request(action);if(mounted.current){setOrder(result);setNotice(action==='cancel'?'Đã hủy đơn hàng.':'Đã xác nhận nhận hàng.');}}
    catch(e){if(mounted.current && getSession()===session && e.name!=='AbortError'){setStale(true);setError(e.message==='Failed to fetch'?'Chưa xác nhận được kết quả. Hãy tải lại đơn trước khi thao tác tiếp.':e.message);}}
    finally{lock.current=false;if(mounted.current)setBusy(false);}
  }
  return <><header className="cart-header"><a className="brand" href="/" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt=""/><span>SHOP<span>FLOW</span><i/></span></a><nav><a href="/account">Tài khoản</a><a href="/catalog">Tiếp tục mua sắm</a></nav></header><main className="cart-page order-page" tabIndex="-1">
    <a className="order-back" href="/account">← Đơn hàng của tôi</a><h1>Chi tiết đơn hàng{order?` #${order.id}`:''}</h1>
    {!session?<section className="cart-feedback"><h2>Đăng nhập để xem đơn hàng</h2><a className="cart-primary" href={`/login?next=${encodeURIComponent(`/orders/${id}`)}`}>Đăng nhập</a></section>:session.user.role!=='Customer'?<section className="cart-feedback"><h2>Đơn mua hàng dành cho khách hàng</h2><a href="/account">Về tài khoản</a></section>:<>
      {error&&<div className="cart-alert" role="alert">{error} <button disabled={busy} onClick={()=>setRetry(n=>n+1)}>Tải lại đơn</button></div>}
      {notice&&<p className="cart-success" role="status">{notice}</p>}
      {loading?<p role="status">Đang tải đơn hàng…</p>:order&&<>
        <section className={`order-progress ${order.status==='Cancelled'?'is-cancelled':''}`}><div className="order-status-heading"><div><span className="cart-eyebrow">TRẠNG THÁI ĐƠN HÀNG</span><h2>{order.status==='Cancelled'?'Đã hủy':stages.find(([value])=>value===order.status)?.[1]||order.status}</h2></div><button disabled={busy} onClick={()=>setRetry(n=>n+1)}>Làm mới</button></div><p>{descriptions[order.status]}</p>{order.status!=='Cancelled'&&<ol>{stages.map(([value,label],index)=><li key={value} className={index<=stages.findIndex(([value])=>value===order.status)?'reached':''} aria-current={value===order.status?'step':undefined}><span aria-hidden="true">{index+1}</span>{label}</li>)}</ol>}</section>
        <div className="order-layout"><section className="order-products"><h2>Sản phẩm trong đơn</h2><ul className="checkout-lines">{order.items.map(item=><li key={item.productId}><div><strong>{item.productName}</strong><span>{item.sku}</span><p>{item.quantity} × {money(item.unitPrice)}</p></div><b>{money(item.lineTotal)}</b></li>)}</ul><div className="checkout-total"><span>Tổng tiền</span><strong>{money(order.totalAmount)}</strong></div></section><aside className="order-information"><h2>Thông tin giao hàng</h2><dl><dt>Ngày đặt</dt><dd>{new Intl.DateTimeFormat('vi-VN',{dateStyle:'long',timeStyle:'short',timeZone:'Asia/Ho_Chi_Minh'}).format(new Date(order.createdAt))}</dd><dt>Địa chỉ nhận hàng</dt><dd>{order.shippingAddress}</dd></dl>{action&&<div className="order-actions"><button className="cart-primary" disabled={busy||stale} onClick={()=>dialog.current.showModal()}>{busy?'Đang xử lý…':action==='cancel'?'Hủy đơn hàng':'Đã nhận hàng'}</button><p>{action==='cancel'?'Bạn có thể hủy khi đơn còn chờ thanh toán.':'Xác nhận khi bạn đã nhận đủ sản phẩm trong đơn.'}</p></div>}</aside></div>
      </>}
    </>}
    <dialog className="cart-confirm" ref={dialog} aria-labelledby="order-confirm-title"><h2 id="order-confirm-title">{action==='cancel'?'Hủy đơn hàng này?':'Bạn đã nhận đủ hàng?'}</h2><p>{action==='cancel'?'Đơn sẽ được hủy. Bạn có thể chọn sản phẩm và đặt lại một đơn mới.':'Sau khi xác nhận, đơn hàng sẽ chuyển sang trạng thái hoàn tất.'}</p><div><button onClick={()=>dialog.current.close()} autoFocus>Quay lại</button><button className="cart-primary" onClick={confirm} disabled={busy}>Xác nhận</button></div></dialog>
  </main></>;
}

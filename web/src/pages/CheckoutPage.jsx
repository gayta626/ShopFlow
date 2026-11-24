import { useEffect, useRef, useState } from 'react';
import { clearSession, getSession, useSession } from '../lib/auth';
import { cartRequest } from '../lib/cart';
import { money } from '../lib/api';
import '../cart.css';
import '../checkout.css';

// Keep an in-flight or uncertain checkout across internal navigation in this session.
const attempts = new WeakMap();
const uncertainMessage = 'Chưa xác nhận được kết quả đặt hàng. Hãy kiểm tra đơn trong tài khoản trước khi đặt lại.';

export default function CheckoutPage() {
  const session=useSession(), [cart,setCart]=useState(null), [loading,setLoading]=useState(true), [error,setError]=useState(''), [address,setAddress]=useState(''), [busy,setBusy]=useState(false), [blocked,setBlocked]=useState(false), [retry,setRetry]=useState(0), [order,setOrder]=useState(null);
  const [payment,setPayment]=useState('demo');
  const lock=useRef(false), mounted=useRef(true), alert=useRef(null);
  useEffect(()=>{document.title='Đặt hàng | ShopFlow';mounted.current=true;return()=>{mounted.current=false;};},[]);
  useEffect(()=>{if(error)alert.current?.focus();},[error]);
  useEffect(()=>{
    setCart(null);setOrder(null);setError('');setLoading(true);setBlocked(false);
    if(!session || session.user.role!=='Customer'){setLoading(false);return;}
    const previous=attempts.get(session);
    if(previous?.order){setOrder(previous.order);setLoading(false);return;}
    if(previous){setError(uncertainMessage);setBlocked(true);setLoading(false);return;}
    const controller=new AbortController();
    cartRequest('GET',undefined,undefined,controller.signal).then(setCart).catch(error=>{if(error.name!=='AbortError')setError(error.status?error.message:'Chưa tải được giỏ hàng. Vui lòng thử lại.');}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return()=>controller.abort();
  },[session,retry]);
  async function submit(event){
    event.preventDefault();if(payment!=='demo')return;if(lock.current || busy || blocked || !session || !cart?.items.length || cart.items.some(item=>!item.isAvailable))return;
    const shippingAddress=address.trim();
    if(shippingAddress.length<10 || shippingAddress.length>500){setError('Địa chỉ giao hàng cần từ 10 đến 500 ký tự sau khi bỏ khoảng trắng đầu cuối.');return;}
    if(attempts.has(session)){setBlocked(true);setError(uncertainMessage);return;}
    lock.current=true;setBusy(true);setError('');attempts.set(session,{pending:true});
    try{
      const response=await fetch('/api/orders',{method:'POST',headers:{Accept:'application/json','Content-Type':'application/json',Authorization:`Bearer ${session.accessToken}`},body:JSON.stringify({shippingAddress})});
      if(getSession()!==session)return;
      if(response.status===201){
        const result=await response.json();
        if(!result.id || !Array.isArray(result.items))throw new Error('Invalid response');
        attempts.set(session,{order:result});if(mounted.current){setOrder(result);setAddress('');setCart(null);}return;
      }
      const body=await response.json().catch(()=>({}));
      if(response.status===401){attempts.delete(session);clearSession();return;}
      if(response.status>=500)throw new Error('Unknown outcome');
      attempts.delete(session);
      const messages={INSUFFICIENT_STOCK:'Một sản phẩm không đủ tồn kho. Quay lại giỏ để cập nhật số lượng.',PRODUCT_UNAVAILABLE:'Một sản phẩm đã ngừng bán. Quay lại giỏ để kiểm tra.',CONCURRENCY_CONFLICT:'Giỏ hàng hoặc tồn kho vừa thay đổi. Hãy tải lại giỏ trước khi xác nhận.'};
      if(mounted.current){setBlocked(response.status!==400);setError(response.status===409?(messages[body.code]||'Chưa thể đặt hàng. Hãy kiểm tra lại giỏ.'):response.status===400?'Địa chỉ chưa hợp lệ hoặc giỏ hàng đang trống. Kiểm tra địa chỉ và tải lại giỏ.':response.status===403?'Chỉ tài khoản khách hàng được đặt hàng.':'Không tìm thấy giỏ hàng. Hãy quay lại giỏ để kiểm tra.');}
    }catch{
      attempts.set(session,{uncertain:true});if(mounted.current && getSession()===session){setBlocked(true);setError(uncertainMessage);}
    }finally{lock.current=false;if(mounted.current)setBusy(false);}
  }
  const restart=()=>{if(session && attempts.get(session)?.order)attempts.delete(session);setRetry(value=>value+1);};
  return <><header className="cart-header"><a className="brand" href="/" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt="" /><span>SHOP<span>FLOW</span><i /></span></a><nav><a href="/cart">Giỏ hàng</a><a href="/account">Tài khoản</a></nav></header><main className="checkout-page" id="main" tabIndex="-1"><span className="cart-eyebrow">SHOPFLOW / {order?'ĐƠN HÀNG':'ĐẶT HÀNG'}</span><h1>{order?'Đã tạo đơn hàng.':'Sắp đến góc bàn của bạn.'}</h1>
    {!session?<section className="cart-feedback"><h2>Đăng nhập để đặt hàng</h2><p>Nếu vừa gửi đơn rồi mất phiên, hãy kiểm tra mục đơn hàng sau khi đăng nhập lại.</p><a className="cart-primary" href="/login?next=%2Fcheckout">Đăng nhập</a></section>:session.user.role!=='Customer'?<section className="cart-feedback"><h2>Đặt hàng dành cho khách hàng</h2><a href="/account">Về tài khoản</a></section>:order?<section className="checkout-result"><div className="checkout-result-heading"><span className="checkout-check" aria-hidden="true">✓</span><div><h2>Đơn #{order.id}</h2><p role="status">Đặt hàng thành công · {order.status==='Pending'?'Chờ thanh toán':order.status}</p></div></div><p>Đơn đã được ghi nhận. Chưa có khoản tiền nào được thanh toán qua trang này.</p><div className="checkout-address"><h3>Địa chỉ nhận hàng</h3><p>{order.shippingAddress}</p></div><OrderLines items={order.items} /><div className="checkout-total"><span>Tổng tiền đơn hàng</span><strong>{money(order.totalAmount)}</strong></div><div className="checkout-result-actions"><a className="cart-primary" href={`/orders/${order.id}`}>Xem chi tiết đơn hàng</a><a href="/catalog">Tiếp tục mua sắm</a><button onClick={restart}>Đặt đơn mới từ giỏ</button></div></section>:<>
      {error&&<div className="cart-alert" role="alert" tabIndex="-1" ref={alert}>{error}<div className="checkout-error-actions"><a href="/account">Kiểm tra đơn của tôi</a><a href="/cart">Quay lại giỏ</a>{!attempts.has(session)&&<button disabled={busy} onClick={()=>setRetry(value=>value+1)}>Tải lại giỏ</button>}</div></div>}
      {loading?<p role="status" className="cart-feedback">Đang kiểm tra giỏ hàng…</p>:cart&&!cart.items.length?<section className="cart-feedback"><h2>Giỏ hàng đang trống.</h2><a className="cart-primary" href="/catalog">Khám phá bàn phím</a></section>:cart?<div className="checkout-layout"><section className="checkout-form-panel"><span className="auth-eyebrow">THÔNG TIN NHẬN HÀNG</span><h2>Giao đến đâu?</h2><form onSubmit={submit} id="checkout-form" aria-busy={busy}><label htmlFor="shipping-address">Địa chỉ giao hàng</label><textarea id="shipping-address" name="shippingAddress" autoComplete="street-address" required minLength="10" maxLength="500" rows="5" value={address} onChange={event=>setAddress(event.target.value)} disabled={busy} aria-describedby="address-hint" placeholder="Số nhà, đường, phường/xã, tỉnh/thành phố" /><p id="address-hint">Nhập đầy đủ địa chỉ nhận hàng, từ 10–500 ký tự.</p><fieldset className="checkout-payment payment-methods"><legend>Phương thức thanh toán</legend><label className={`payment-option ${payment==='card'?'selected':''}`}><input type="radio" name="paymentMethod" value="card" checked={payment==='card'} onChange={()=>setPayment('card')} disabled={busy} /><span className="payment-icon" aria-hidden="true"><svg viewBox="0 0 32 24" fill="none"><rect x="1" y="1" width="30" height="22" rx="4" stroke="currentColor" strokeWidth="1.5"/><path d="M2 8h28M6 17h7" stroke="currentColor" strokeWidth="2"/></svg></span><span className="payment-option-copy"><strong>Thẻ tín dụng / ghi nợ</strong><small>Thanh toán qua cổng thanh toán</small></span><span className="payment-soon">Sắp hỗ trợ</span></label><label className={`payment-option ${payment==='demo'?'selected':''}`}><input type="radio" name="paymentMethod" value="demo" checked={payment==='demo'} onChange={()=>setPayment('demo')} disabled={busy}/><span className="payment-icon" aria-hidden="true">✓</span><span className="payment-option-copy"><strong>Đặt hàng demo</strong><small>Tạo đơn chờ thanh toán, không thu tiền</small></span></label>{payment==='card'?<div className="payment-card-preview" role="status"><div className="payment-preview-heading"><strong>Thanh toán bằng thẻ</strong><span>Đang chuẩn bị</span></div><p>Sau khi tích hợp, bạn sẽ chuyển sang cổng thanh toán để nhập thông tin thẻ và xác nhận giao dịch.</p><div className="payment-steps"><span>1. Xác nhận đơn</span><span>2. Thanh toán tại cổng</span><span>3. Nhận kết quả</span></div><p className="payment-preview-note">Thanh toán thẻ hiện chưa hoạt động. Chọn “Đặt hàng demo” nếu bạn muốn thử luồng tạo đơn.</p></div>:<p className="payment-demo-note">Đơn được tạo ở trạng thái chờ thanh toán. Bạn không bị trừ tiền khi xác nhận đơn demo.</p>}</fieldset><p className="checkout-price-note">Giá và tồn kho được kiểm tra khi tạo đơn. Tổng tiền cuối cùng được hiển thị trong kết quả đặt hàng.</p></form></section><aside className="checkout-summary"><div className="checkout-summary-heading"><h2>Đơn hàng của bạn</h2><a href="/cart">Sửa giỏ</a></div><OrderLines items={cart.items} /><div className="checkout-total"><span>Tạm tính</span><strong>{money(cart.totalAmount)}</strong></div><div className="checkout-shipping"><span>Phí giao hàng</span><span>0 ₫</span></div>{cart.items.some(item=>!item.isAvailable)&&<p className="cart-alert">Có sản phẩm hết hàng, thiếu tồn kho hoặc ngừng bán. Quay lại giỏ để cập nhật trước khi đặt.</p>}<button form="checkout-form" className="cart-primary" disabled={busy||blocked||payment!=='demo'||cart.items.some(item=>!item.isAvailable)}>{busy?'Đang tạo đơn…':payment==='card'?'Thanh toán thẻ — sắp hỗ trợ':'Xác nhận đặt hàng'}</button><small>{payment==='card'?'Chưa tạo đơn hoặc thu tiền khi chọn phương thức này.':'Nhấn xác nhận để đặt toàn bộ sản phẩm trong giỏ.'}</small></aside></div>:null}
    </>}
  </main><footer><a className="footer-brand" href="/">SHOPFLOW</a><span>Đặt hàng demo · Thanh toán mô phỏng</span></footer></>;
}

function OrderLines({items}){
  return <ul className="checkout-lines">{items.map(item=><li key={item.productId}><div><strong>{item.productName}</strong><span>{item.sku}</span><p>{item.quantity} × {money(item.unitPrice)}</p></div><b>{money(item.lineTotal)}</b></li>)}</ul>;
}



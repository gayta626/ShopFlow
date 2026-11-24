import { navigate } from '../lib/navigation';
import { useEffect, useRef, useState } from 'react';
import { useSession } from '../lib/auth';
import { cartRequest } from '../lib/cart';
import { money } from '../lib/api';
import { productImage } from '../lib/productImages';
import '../cart.css';

function CartRow({item,busy,onUpdate,onRemove}) {
  const [quantity,setQuantity] = useState(String(item.quantity));
  useEffect(()=>setQuantity(String(item.quantity)),[item.quantity]);
  return <article className="cart-row"><a className="cart-photo" href={`/products/${item.productId}`}><img src={productImage({sku:item.sku})} alt={`Ảnh minh họa cho ${item.productName}`} /><span>Ảnh minh họa</span></a><div className="cart-row-info"><span className="cart-sku">{item.sku}</span><h2><a href={`/products/${item.productId}`}>{item.productName}</a></h2><p>{money(item.unitPrice)} / chiếc</p>{!item.isAvailable && <p className="cart-unavailable">Chưa đủ điều kiện mua: thiếu tồn kho hoặc đã ngừng bán.</p>}<span className="cart-stock">Tồn hiện tại: {item.stock}</span><form onSubmit={event=>{event.preventDefault();const count=Number(quantity);if(Number.isInteger(count)&&count>=1&&count<=99)onUpdate(item.productId,count);}}><label>Số lượng<input type="number" min="1" max="99" step="1" required aria-label={`Số lượng ${item.productName}`} value={quantity} onChange={event=>setQuantity(event.target.value)} disabled={busy} /></label><button type="submit" disabled={busy || Number(quantity) === item.quantity}>Cập nhật</button><button type="button" className="cart-remove" disabled={busy} onClick={()=>onRemove(item)}>Xóa</button></form></div><strong className="cart-line-total">{money(item.lineTotal)}</strong></article>;
}

export default function CartPage() {
  const session = useSession(), [cart,setCart] = useState(null), [busy,setBusy] = useState(false), [error,setError] = useState(''), [notice,setNotice] = useState(''), [reload,setReload] = useState(0), [stale,setStale] = useState(false), [removing,setRemoving] = useState(null);
  const request = useRef(null), lock = useRef(false), dialog = useRef(null);
  useEffect(()=>{document.title='Giỏ hàng | ShopFlow';},[]);
  useEffect(()=>{
    setCart(null);setError('');setNotice('');setStale(false);
    if (!session || session.user.role !== 'Customer') return;
    const controller = new AbortController();request.current=controller;setBusy(true);lock.current=true;
    cartRequest('GET',undefined,undefined,controller.signal).then(setCart).catch(error=>{if(error.name!=='AbortError')setError(error.status ? error.message : 'Không tải được giỏ hàng. Vui lòng thử lại.');}).finally(()=>{if(!controller.signal.aborted){setBusy(false);lock.current=false;}});
    return ()=>controller.abort();
  },[session,reload]);
  useEffect(()=>()=>request.current?.abort(),[]);
  useEffect(()=>{if(removing && session)dialog.current?.showModal();},[removing,session]);
  async function update(method,id,quantity) {
    if(lock.current)return;lock.current=true;setBusy(true);setError('');setNotice('');
    request.current=new AbortController();
    try {
      const next=await cartRequest(method,id,quantity,request.current.signal);
      if(next)setCart(next);else setReload(value=>value+1);
      setNotice(method==='DELETE'?'Đã xóa sản phẩm khỏi giỏ.':'Đã cập nhật số lượng.');
    } catch(error) {if(error.name!=='AbortError'){setError(error.status ? error.message : 'Chưa xác nhận được kết quả cập nhật. Hãy tải lại giỏ trước khi thử lại.');setStale(true);}}
    finally{setBusy(false);lock.current=false;}
  }
  return <><header className="cart-header"><a className="brand" href="/" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt="" /><span>SHOP<span>FLOW</span><i /></span></a><nav><a href="/catalog">Tiếp tục mua sắm</a><a href="/account">Tài khoản</a></nav></header><main className="cart-page" id="main" tabIndex="-1"><span className="cart-eyebrow">SHOPFLOW / GIỎ HÀNG</span><h1>Những lựa chọn của bạn.</h1>
    {!session ? <section className="cart-feedback"><h2>Đăng nhập để xem giỏ hàng</h2><p>Giỏ hàng được lưu theo tài khoản. Tải lại trang sẽ cần đăng nhập lại.</p><a className="cart-primary" href="/login?next=%2Fcart">Đăng nhập</a></section> : session.user.role !== 'Customer' ? <section className="cart-feedback"><h2>Giỏ hàng dành cho khách hàng</h2><p>Tài khoản quản trị không dùng để mua hàng. Hãy đăng nhập bằng tài khoản khách hàng.</p><a href="/account">Về tài khoản</a></section> : <>
      {error && <div className="cart-alert" role="alert">{error} <button disabled={busy} onClick={()=>setReload(value=>value+1)}>Tải lại giỏ</button></div>}{notice && <p className="cart-success" role="status">{notice}</p>}
      {!cart ? busy ? <p role="status" className="cart-feedback">Đang tải giỏ hàng…</p> : null : !cart.items.length ? <section className="cart-feedback"><h2>Giỏ hàng đang trống.</h2><p>Bắt đầu với chiếc bàn phím phù hợp góc làm việc của bạn.</p><a className="cart-primary" href="/catalog">Khám phá bàn phím</a></section> : <div className="cart-layout" aria-busy={busy}><section className="cart-items" aria-label="Sản phẩm trong giỏ"><div className="cart-items-heading"><span>{cart.items.length} sản phẩm khác nhau</span><button disabled={busy} onClick={()=>setReload(value=>value+1)}>Làm mới giá và tồn kho</button></div>{cart.items.map(item=><CartRow key={`${item.productId}-${reload}`} item={item} busy={busy || stale} onUpdate={(id,count)=>update('PUT',id,count)} onRemove={setRemoving} />)}</section><aside className="cart-summary"><h2>Tóm tắt giỏ hàng</h2><div><span>Số lượng</span><strong>{cart.items.reduce((sum,item)=>sum+item.quantity,0)}</strong></div><div className="cart-total"><span>Tạm tính</span><strong>{money(cart.totalAmount)}</strong></div><p>Giá và tồn kho có thể thay đổi. Giỏ hàng chưa giữ hàng.</p>{cart.items.some(item=>!item.isAvailable)&&<p className="cart-unavailable">Có sản phẩm chưa đủ điều kiện mua. Cập nhật số lượng hoặc xóa sản phẩm đó.</p>}<button className="cart-primary" disabled={busy || stale || cart.items.some(item=>!item.isAvailable)} onClick={()=>navigate('/checkout')}>Tiến hành đặt hàng</button><small>Kiểm tra địa chỉ và xác nhận ở bước tiếp theo.</small><a href="/catalog">Tiếp tục chọn bàn phím</a></aside></div>}
      <dialog ref={dialog} className="cart-confirm" aria-labelledby="remove-title" onClose={()=>setRemoving(null)} onClick={event=>{if(event.target===dialog.current)dialog.current.close();}}><h2 id="remove-title">Xóa sản phẩm khỏi giỏ?</h2><p>{removing?.productName}</p><div><button autoFocus onClick={()=>dialog.current.close()}>Giữ lại</button><button className="cart-primary" onClick={()=>{const id=removing.productId;dialog.current.close();update('DELETE',id);}}>Xóa sản phẩm</button></div></dialog>
    </>}
    </main><footer><a className="footer-brand" href="/">SHOPFLOW</a><span>Ảnh minh họa · Giá và tồn kho theo cửa hàng</span></footer></>;
}


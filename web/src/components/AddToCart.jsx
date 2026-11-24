import { useEffect, useRef, useState } from 'react';
import { useSession } from '../lib/auth';
import { cartRequest } from '../lib/cart';
import { navigate } from '../lib/navigation';

export default function AddToCart({product}) {
  const session = useSession(), lock = useRef(false), request = useRef(null);
  const [quantity,setQuantity] = useState('1'), [busy,setBusy] = useState(false), [message,setMessage] = useState(''), [failed,setFailed] = useState(false);
  useEffect(() => () => request.current?.abort(), []);
  useEffect(() => { setMessage(''); }, [session]);
  async function add(event) {
    event.preventDefault(); if (lock.current) return;
    if (!session) { navigate(`/login?next=${encodeURIComponent(`/products/${product.id}`)}`); return; }
    if (session.user.role !== 'Customer') return;
    const count = Number(quantity);
    if (!Number.isInteger(count) || count < 1 || count > Math.min(99,product.stock)) { setFailed(true); setMessage('Số lượng phải từ 1 đến 99 và không vượt tồn kho.'); return; }
    lock.current = true; setBusy(true); setMessage(''); request.current = new AbortController();
    try {
      await cartRequest('POST',product.id,count,request.current.signal);
      setFailed(false); setMessage('Đã thêm sản phẩm vào giỏ hàng.');
    } catch(error) { if(error.name !== 'AbortError') { setFailed(true); setMessage(error.status ? error.message : 'Chưa xác nhận được kết quả. Hãy xem giỏ trước khi thêm lại để tránh tăng số lượng hai lần.'); } }
    finally {lock.current = false; setBusy(false);}
  }
  return <div className="add-cart"><form onSubmit={add} aria-busy={busy}><label>Số lượng<input aria-label="Số lượng thêm vào giỏ" type="number" min="1" max={Math.max(1,Math.min(99,product.stock))} step="1" required value={quantity} onChange={e=>setQuantity(e.target.value)} disabled={busy || product.stock < 1} /></label><button className="detail-primary" disabled={busy || product.stock < 1 || (session && session.user.role !== 'Customer')}>{product.stock < 1 ? 'Tạm hết hàng' : busy ? 'Đang thêm…' : !session ? 'Đăng nhập để thêm giỏ' : 'Thêm vào giỏ hàng'}</button></form><p className="purchase-note">{session?.user.role === 'Admin' ? 'Dùng tài khoản khách hàng để mua sản phẩm.' : 'Thêm vào giỏ chưa giữ hàng hoặc giữ giá.'}</p>{message && <p role={failed ? 'alert' : 'status'} className={failed ? 'cart-alert' : 'cart-success'}>{message} <a href="/cart">Xem giỏ hàng</a></p>}</div>;
}

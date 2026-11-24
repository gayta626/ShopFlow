import { useEffect, useState } from 'react';
import { clearSession, getSession, useSession } from '../lib/auth';
import { money } from '../lib/api';

const statuses = [['Pending','Chờ thanh toán'],['Paid','Đã thanh toán'],['Shipped','Đang giao'],['Completed','Hoàn tất'],['Cancelled','Đã hủy']];
async function readOrders(query, signal) {
  const session=getSession();
  if(!session)throw new DOMException('Session ended','AbortError');
  const response=await fetch(`/api/orders/me?${query}`,{signal,headers:{Accept:'application/json',Authorization:`Bearer ${session.accessToken}`}});
  if(getSession()!==session)throw new DOMException('Session changed','AbortError');
  if(response.status===401){clearSession();throw new DOMException('Session ended','AbortError');}
  if(!response.ok)throw new Error(response.status===403?'Tài khoản này không có quyền xem đơn mua hàng.':'Chưa tải được đơn hàng. Vui lòng thử lại.');
  return response.json();
}
export default function ProfileOrders() {
  const session=useSession(), [counts,setCounts]=useState(null), [countError,setCountError]=useState('');
  const [data,setData]=useState(null), [error,setError]=useState(''), [status,setStatus]=useState(''), [page,setPage]=useState(1), [retry,setRetry]=useState(0);
  useEffect(()=>{
    const controller=new AbortController();setCounts(null);setCountError('');
    Promise.all(['',...statuses.map(([value])=>value)].map(status=>readOrders(new URLSearchParams({page:1,size:1,...(status?{status}:{})}),controller.signal))).then(values=>setCounts(values.map(value=>value.totalCount))).catch(error=>{if(error.name!=='AbortError')setCountError(error.message);});
    return ()=>controller.abort();
  },[session,retry]);
  useEffect(()=>{
    const controller=new AbortController();setData(null);setError('');
    readOrders(new URLSearchParams({page,size:5,...(status?{status}:{})}),controller.signal).then(setData).catch(error=>{if(error.name!=='AbortError')setError(error.message);});
    return ()=>controller.abort();
  },[session,status,page,retry]);
  const choose=value=>{setStatus(value);setPage(1);};
  return <section className="profile-orders" aria-labelledby="orders-heading"><div className="profile-orders-heading"><div><span className="auth-eyebrow">MUA SẮM CỦA BẠN</span><h2 id="orders-heading">Tổng quan đơn hàng</h2></div><button onClick={()=>setRetry(value=>value+1)}>Làm mới</button></div>
    {countError?<p className="auth-error" role="alert">{countError}</p>:<div className="order-counts" aria-busy={!counts}>{[['','Tất cả'],...statuses].map(([value,label],index)=><button key={value} aria-pressed={status===value} onClick={()=>choose(value)}><span>{label}</span><strong>{counts?counts[index]:'—'}</strong></button>)}</div>}
    <div className="profile-order-list-heading"><h3>{status?statuses.find(([value])=>value===status)?.[1]:'Đơn hàng gần đây'}</h3><span>Mới nhất trước</span></div>
    {error?<div className="auth-error" role="alert">{error}<button onClick={()=>setRetry(value=>value+1)}>Thử lại đơn hàng</button></div>:!data?<p role="status" className="profile-order-empty">Đang tải đơn hàng…</p>:!data.items.length?<div className="profile-order-empty"><h3>{status?'Chưa có đơn ở trạng thái này.':'Bạn chưa có đơn hàng nào.'}</h3><p>{status?'Chọn trạng thái khác để xem đơn của bạn.':'Khám phá bộ sưu tập và chọn chiếc bàn phím đầu tiên.'}</p><a href="/catalog">Khám phá bàn phím</a></div>:<><div className="profile-order-table"><table><thead><tr><th>Mã đơn</th><th>Ngày đặt</th><th>Trạng thái</th><th>Tổng tiền</th></tr></thead><tbody>{data.items.map(order=><tr key={order.id}><td><a href={`/orders/${order.id}`} aria-label={`Xem đơn #${order.id}`}><strong>#{order.id} ↗</strong></a></td><td>{new Intl.DateTimeFormat('vi-VN',{dateStyle:'short',timeZone:'Asia/Ho_Chi_Minh'}).format(new Date(order.createdAt))}</td><td><span className={`order-status status-${order.status}`}>{statuses.find(([value])=>value===order.status)?.[1]||order.status}</span></td><td>{money(order.totalAmount)}</td></tr>)}</tbody></table></div><nav className="profile-order-pagination" aria-label="Phân trang đơn hàng"><span>{data.totalCount} đơn · Trang {data.page}/{Math.max(1,Math.ceil(data.totalCount/data.size))}</span><div><button disabled={page<=1} onClick={()=>setPage(value=>value-1)}>Trước</button><button disabled={page*data.size>=data.totalCount} onClick={()=>setPage(value=>value+1)}>Tiếp</button></div></nav></>}
  </section>;
}


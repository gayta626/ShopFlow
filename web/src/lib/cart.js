import { clearSession, getSession } from './auth';

export async function cartRequest(method = 'GET', productId, quantity, signal) {
  const session = getSession();
  if (!session || Date.parse(session.expiresAt) <= Date.now()) {
    clearSession(); throw new Error('Bạn cần đăng nhập lại để sử dụng giỏ hàng.');
  }
  const path = method === 'PUT' || method === 'DELETE' ? `/${productId}` : '';
  const response = await fetch(`/api/cart/items${path}`, {
    method, signal, headers: {Accept:'application/json', Authorization:`Bearer ${session.accessToken}`, ...(quantity !== undefined ? {'Content-Type':'application/json'} : {})},
    ...(quantity !== undefined ? {body:JSON.stringify(method === 'POST' ? {productId,quantity} : {quantity})} : {}),
  });
  if (session !== getSession()) throw new DOMException('Phiên đăng nhập đã thay đổi', 'AbortError');
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    if (response.status === 401) clearSession();
    const message = response.status === 401 ? 'Phiên đã hết hạn. Vui lòng đăng nhập lại.' : response.status === 403 ? 'Giỏ hàng chỉ dành cho tài khoản khách hàng.' : response.status === 404 ? 'Giỏ hàng hoặc sản phẩm không còn tồn tại. Hãy tải lại giỏ.' : response.status === 409 ? (body.code === 'CONCURRENCY_CONFLICT' ? 'Giỏ hàng vừa thay đổi. Hãy tải lại trước khi thao tác tiếp.' : 'Chưa thể cập nhật: sản phẩm có thể ngừng bán, thiếu tồn kho hoặc vượt giới hạn giỏ. Hãy tải lại để kiểm tra.') : response.status === 400 ? 'Số lượng phải là số nguyên từ 1 đến 99.' : 'Chưa thể kết nối cửa hàng. Hãy tải lại giỏ để kiểm tra kết quả trước khi thử lại.';
    const error = new Error(message); error.status = response.status; throw error;
  }
  return response.status === 204 ? null : response.json();
}

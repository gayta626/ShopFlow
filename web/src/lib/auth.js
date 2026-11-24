import { useSyncExternalStore } from 'react';

let session = null, expiryTimer;
const listeners = new Set();
const subscribe = listener => { listeners.add(listener); return () => listeners.delete(listener); };
export const useSession = () => useSyncExternalStore(subscribe, () => session);
export const getSession = () => session;
export function clearSession() {
  clearTimeout(expiryTimer); session = null; listeners.forEach(listener => listener());
}
export function saveSession(value) {
  const remaining = Date.parse(value.expiresAt) - Date.now();
  if (!value.accessToken || !value.user?.id || !Number.isFinite(remaining) || remaining <= 0) throw new Error('Phiên đăng nhập không hợp lệ. Vui lòng thử lại.');
  clearTimeout(expiryTimer); session = value;
  expiryTimer = setTimeout(clearSession, Math.min(remaining, 2147483647));
  listeners.forEach(listener => listener());
}
export function updateSessionProfile(user, expectedSession) {
  if (!expectedSession || session !== expectedSession) return false;
  if (user?.id !== session.user.id || user.role !== session.user.role || typeof user.fullName !== 'string' || typeof user.email !== 'string')
    throw new Error('Thông tin hồ sơ trả về không hợp lệ. Vui lòng tải lại hồ sơ.');
  if (user.fullName !== session.user.fullName || user.email !== session.user.email)
    saveSession({...session, user});
  return true;
}
export async function authRequest(path, body, signal, method) {
  const current = session;
  const response = await fetch(`/api/auth/${path}`, {
    method: method || (body ? 'POST' : 'GET'), signal,
    headers: { Accept: 'application/json', ...(body ? {'Content-Type':'application/json'} : {}), ...(path === 'me' && current ? {Authorization:`Bearer ${current.accessToken}`} : {}) },
    ...(body ? {body:JSON.stringify(body)} : {}),
  });
  if (path === 'me' && session !== current) throw new DOMException('Session changed', 'AbortError');
  if (!response.ok) {
    if (path === 'me' && response.status === 401 && session === current) clearSession();
    const error = new Error(response.status === 401 ? (path === 'login' ? 'Email hoặc mật khẩu không đúng.' : 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.') : response.status === 409 ? 'Email đã được sử dụng. Hãy đăng nhập hoặc dùng email khác.' : response.status === 400 ? 'Thông tin chưa hợp lệ. Hãy kiểm tra các trường và thử lại.' : 'Chưa thể kết nối cửa hàng. Vui lòng thử lại.');
    error.status = response.status; throw error;
  }
  const result = await response.json();
  if (path === 'me' && session !== current) throw new DOMException('Session changed', 'AbortError');
  return result;
}

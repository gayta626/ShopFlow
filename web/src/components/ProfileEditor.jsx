import { useEffect, useRef, useState } from 'react';
import { authRequest, getSession, updateSessionProfile } from '../lib/auth';

export default function ProfileEditor({user}) {
  const dialog = useRef(null), lock = useRef(false), alive = useRef(true);
  const [name,setName] = useState(user.fullName), [email,setEmail] = useState(user.email);
  const [busy,setBusy] = useState(false), [error,setError] = useState('');
  const [uncertain,setUncertain] = useState(false), [success,setSuccess] = useState('');
  useEffect(() => { alive.current = true; return () => { alive.current = false; }; }, []);
  function open() {
    setName(user.fullName); setEmail(user.email); setError(''); setSuccess('');
    dialog.current.showModal();
  }
  async function reload() {
    if (lock.current) return;
    lock.current = true; setBusy(true); setError('');
    const current = getSession();
    try {
      const result = await authRequest('me');
      if (!alive.current || !updateSessionProfile(result, current)) return;
      setName(result.fullName); setEmail(result.email); setUncertain(false);
    } catch (error) {
      if (alive.current && error.name !== 'AbortError') setError('Chưa tải được hồ sơ. Vui lòng thử lại hoặc đăng nhập lại.');
    } finally { lock.current = false; if (alive.current) setBusy(false); }
  }
  async function submit(event) {
    event.preventDefault();
    if (lock.current || uncertain) return;
    const fullName = name.trim(), trimmedEmail = email.trim();
    if (fullName.length < 2 || fullName.length > 100) { setError('Họ tên cần từ 2–100 ký tự sau khi bỏ khoảng trắng.'); return; }
    if (!trimmedEmail || trimmedEmail.length > 254) { setError('Email không được để trống và không quá 254 ký tự.'); return; }
    lock.current = true; setBusy(true); setError(''); setSuccess('');
    const current = getSession();
    try {
      const result = await authRequest('me', {fullName,email:trimmedEmail}, undefined, 'PUT');
      if (!alive.current || !updateSessionProfile(result, current)) return;
      dialog.current.close(); setSuccess('Đã lưu thông tin cá nhân.');
    } catch (error) {
      if (!alive.current || error.name === 'AbortError') return;
      if (error.status === 409) setError('Email đã được tài khoản khác sử dụng. Hãy chọn email khác.');
      else if (error.status === 400) setError('Thông tin chưa hợp lệ. Kiểm tra họ tên và email rồi thử lại.');
      else {
        setUncertain(true);
        setError(error.status === 404 ? 'Không tìm thấy tài khoản. Vui lòng đăng nhập lại.' : 'Chưa xác nhận được kết quả lưu. Tải lại hồ sơ để kiểm tra trước khi sửa tiếp.');
      }
    } finally { lock.current = false; if (alive.current) setBusy(false); }
  }
  return <>
    <button className="profile-edit-button" onClick={open}>Chỉnh sửa thông tin</button>
    {success && <p className="auth-success" role="status">Đã lưu thông tin cá nhân.</p>}
    <dialog className="profile-edit-dialog" ref={dialog} aria-labelledby="profile-edit-title" aria-describedby="profile-edit-note" onCancel={event=>{if(lock.current)event.preventDefault();}}>
      <form onSubmit={submit} aria-busy={busy}>
        <div className="profile-edit-heading"><h2 id="profile-edit-title">Chỉnh sửa thông tin</h2><button type="button" aria-label="Đóng chỉnh sửa" disabled={busy} onClick={()=>dialog.current.close()}>×</button></div>
        <p id="profile-edit-note">Email mới sẽ được dùng cho lần đăng nhập tiếp theo. Mật khẩu của bạn được giữ nguyên.</p>
        <label htmlFor="profile-full-name">Họ tên<input id="profile-full-name" autoFocus autoComplete="name" value={name} onChange={event=>setName(event.target.value)} required minLength="2" maxLength="100" disabled={busy || uncertain}/></label>
        <label htmlFor="profile-email">Email<input id="profile-email" type="email" autoComplete="email" value={email} onChange={event=>setEmail(event.target.value)} required maxLength="254" disabled={busy || uncertain}/></label>
        {error && <p className="auth-error" role="alert">{error}</p>}
        {uncertain && <button type="button" className="profile-edit-button" onClick={reload} disabled={busy}>Tải lại hồ sơ</button>}
        <div className="profile-edit-actions"><button type="button" disabled={busy} onClick={()=>dialog.current.close()}>Đóng</button><button className="auth-submit" disabled={busy || uncertain}>{busy ? 'Đang xử lý…' : 'Lưu thay đổi'}</button></div>
      </form>
    </dialog>
  </>;
}

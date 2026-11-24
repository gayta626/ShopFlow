import StoreHeader from '../components/StoreHeader';
import ProfileEditor from '../components/ProfileEditor';
import ProfileOrders from '../components/ProfileOrders';
import { useEffect, useRef, useState } from 'react';
import { authRequest, clearSession, saveSession, updateSessionProfile, useSession } from '../lib/auth';
import { navigate } from '../lib/navigation';
import '../auth.css';
import AccountMenu from '../components/AccountMenu';

export default function AuthPage() {
  const session = useSession();
  const mode = location.pathname.replace(/\/$/, '');
  const register = mode === '/register', account = mode === '/account', welcome = mode === '/welcome';
  const requestedNext = new URLSearchParams(location.search).get('next') || '';
  const next = /^\/(cart|checkout|account|admin\/categories|admin\/orders(?:\/[1-9]\d*)?|admin\/products(?:\/(?:new|[1-9]\d*))?|orders\/[1-9]\d*|products\/[1-9]\d*)$/.test(requestedNext) ? requestedNext : account ? '/account' : '/welcome';
  const [busy,setBusy] = useState(false), [error,setError] = useState(''), [visible,setVisible] = useState(false);
  const [verified,setVerified] = useState(false), [retry,setRetry] = useState(0);
  const request = useRef(null), lock = useRef(false), alert = useRef(null);
  const title = account && session ? 'Tài khoản của bạn.' : register ? 'Tạo tài khoản.' : 'Chào bạn trở lại.';
  useEffect(() => { document.title = `${register ? 'Đăng ký' : account ? 'Tài khoản' : welcome ? 'Chào mừng' : 'Đăng nhập'} | ShopFlow`; return () => request.current?.abort(); }, [register,account,welcome]);
  useEffect(() => { if (error) alert.current?.focus(); }, [error]);
  useEffect(() => {
    if ((!account && !welcome) || !session) return;
    const controller = new AbortController(); setVerified(false); setError('');
    authRequest('me', undefined, controller.signal).then(me => {
      if (me.id !== session.user.id || me.role !== session.user.role) { clearSession(); return; }
      updateSessionProfile(me, session);
      setVerified(true);
    }).catch(error => { if (error.name !== 'AbortError') setError(error.message); });
    return () => controller.abort();
  }, [account,welcome,session?.accessToken,retry]);
  async function submit(event) {
    event.preventDefault(); if (lock.current) return;
    const form = event.currentTarget, data = new FormData(form);
    const email = String(data.get('email')).trim(), password = String(data.get('password'));
    const fullName = register ? String(data.get('fullName')).trim() : '';
    if (register && (fullName.length < 2 || fullName.length > 100)) { setError('Họ tên cần từ 2 đến 100 ký tự sau khi bỏ khoảng trắng đầu cuối.'); return; }
    if (register && (password.length < 8 || password.length > 128)) { setError('Mật khẩu cần từ 8 đến 128 ký tự.'); return; }
    if (register && password !== data.get('confirmPassword')) { setError('Mật khẩu nhập lại chưa khớp.'); return; }
    lock.current = true; setBusy(true); setError('');
    const controller = new AbortController(); request.current = controller;
    try {
      const result = await authRequest(register ? 'register' : 'login', register ? {email,password,fullName} : {email,password}, controller.signal);
      form.reset();
      if (register) navigate('/login?registered=1&next=' + encodeURIComponent(next), true);
      else { saveSession(result); navigate(next, true); }
    } catch (error) {
      if (error.name !== 'AbortError') setError(error.status || error.message?.startsWith('Phiên') ? error.message : 'Không kết nối được cửa hàng. Kiểm tra kết nối rồi thử lại.');
    } finally { lock.current = false; setBusy(false); }
  }
  if (welcome && session) return <div className="welcome-shell"><header className="auth-header"><a href="/" className="brand" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt="" /><span>SHOP<span>FLOW</span><i /></span></a><AccountMenu /></header><main id="main" tabIndex="-1" className="welcome-main"><h1>Hello, <span>{session.user.fullName}</span></h1>{error ? <div className="auth-error" role="alert" ref={alert} tabIndex="-1">{error}<button onClick={()=>setRetry(value=>value+1)}>Thử lại</button></div> : !verified ? <p role="status">Đang xác thực phiên đăng nhập…</p> : <section className="welcome-poster" aria-label="Khám phá bàn phím ShopFlow"><img src="/assets/keychron-banner.jpg" alt="Bàn phím minh họa cho bộ sưu tập ShopFlow" /><div className="welcome-poster-copy"><span>MAKE ROOM FOR YOURSELF</span><h2>Góc riêng.<br />Nhịp gõ riêng.</h2><a href="/catalog" className="welcome-explore">Khám phá bàn phím</a></div><small>Ảnh minh họa Keychron</small></section>}</main></div>;
  if (account && session) return <div className="profile-shell"><StoreHeader /><main className="profile-main" id="main" tabIndex="-1"><span className="auth-eyebrow">SHOPFLOW / HỒ SƠ</span><h1>Tài khoản của bạn</h1>{error ? <div className="auth-error" role="alert" ref={alert} tabIndex="-1">{error}<button onClick={()=>setRetry(value=>value+1)}>Thử lại</button></div> : !verified ? <p role="status">Đang tải thông tin tài khoản…</p> : <div className="profile-dashboard"><section className="profile-card"><div className="profile-identity"><span className="profile-initial">{session.user.fullName.trim().slice(0,1).toUpperCase()}</span><div><h2>{session.user.fullName}</h2><p>{session.user.role==='Admin'?'Quản trị viên':'Khách hàng'}</p></div></div><dl className="auth-account"><div><dt>Họ tên</dt><dd>{session.user.fullName}</dd></div><div><dt>Email</dt><dd>{session.user.email}</dd></div></dl><ProfileEditor user={session.user}/></section>{session.user.role === 'Customer' && <ProfileOrders />}</div>}<div className="profile-actions">{session.user.role === 'Admin' ? <span><a href="/admin/orders">Quản lý đơn hàng</a> · <a href="/admin/products">Quản lý sản phẩm</a> · <a href="/admin/categories">Quản lý danh mục</a></span> : <a href="/cart">Giỏ hàng của tôi</a>}<button onClick={()=>{clearSession();navigate('/login',true);}}>Đăng xuất</button></div></main></div>;
  return <div className="auth-shell">{account ? <StoreHeader /> : <header className="auth-header"><a href="/" className="brand" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt="" /><span>SHOP<span>FLOW</span><i /></span></a><a href="/catalog">Tiếp tục khám phá</a></header>}
    <main id="main" tabIndex="-1" className="auth-main"><section className="auth-visual" aria-label="Góc bàn phím minh họa"><img src="/assets/q1-black.jpg" alt="Góc làm việc với bàn phím, ảnh minh họa" /><div><span>GÓC RIÊNG CỦA BẠN</span><h2>Một chiếc bàn phím.<br />Nhiều ngày cảm hứng.</h2><p>Chọn layout, cảm giác gõ và cấu hình phù hợp.</p></div><small>Ảnh tham khảo Keychron</small></section>
      <section className="auth-panel"><div className="auth-content"><span className="auth-eyebrow">SHOPFLOW / {account ? 'TÀI KHOẢN' : register ? 'BẮT ĐẦU' : 'CHÀO MỪNG'}</span><h1>{title}</h1>
        {account && session ? <><p className="auth-intro">Xin chào, {session.user.fullName}.</p>{error ? <div className="auth-error" role="alert" ref={alert} tabIndex="-1">{error}<button onClick={() => setRetry(value=>value+1)}>Thử lại</button></div> : !verified ? <p role="status">Đang xác thực phiên đăng nhập…</p> : <><p className="auth-success" role="status">Bạn đã đăng nhập.</p><dl className="auth-account"><div><dt>Họ tên</dt><dd>{session.user.fullName}</dd></div><div><dt>Email</dt><dd>{session.user.email}</dd></div><div><dt>Loại tài khoản</dt><dd>{session.user.role === 'Admin' ? 'Quản trị viên' : 'Khách hàng'}</dd></div></dl><a className="auth-submit" href="/catalog">Khám phá bàn phím</a></>}<button className="auth-logout" onClick={() => { clearSession(); navigate('/login', true); }}>Đăng xuất</button><p className="auth-session-note">Tải lại hoặc đóng trang sẽ kết thúc phiên đăng nhập.</p></> : session ? <><p>Bạn đã đăng nhập với tài khoản {session.user.fullName}.</p><a className="auth-submit" href="/account">Xem tài khoản</a></> : <>
          <p className="auth-intro">{register ? 'Tạo tài khoản để chuẩn bị cho lần mua hàng đầu tiên.' : 'Đăng nhập để tiếp tục với tài khoản ShopFlow của bạn.'}</p>
          {(account || welcome) && <p className="auth-notice" role="status">Bạn cần đăng nhập để xem tài khoản. Nếu vừa tải lại trang, hãy đăng nhập lại.</p>}
          {!register && new URLSearchParams(location.search).get('registered') === '1' && <p className="auth-success" role="status">Tạo tài khoản thành công. Bạn có thể đăng nhập ngay.</p>}
          <form onSubmit={submit} aria-busy={busy}>
            {register && <label htmlFor="fullName">Họ tên<input id="fullName" name="fullName" autoComplete="name" required minLength="2" maxLength="100" disabled={busy} /></label>}
            <label htmlFor="email">Email<input id="email" name="email" type="email" autoComplete="username" required maxLength="254" disabled={busy} /></label>
            <label htmlFor="password">Mật khẩu</label><div className="auth-password"><input id="password" name="password" type={visible ? 'text' : 'password'} autoComplete={register ? 'new-password' : 'current-password'} required minLength={register ? 8 : undefined} maxLength={register ? 128 : undefined} disabled={busy} aria-describedby={register ? 'password-hint' : undefined} /><button type="button" aria-label={visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'} aria-pressed={visible} onClick={()=>setVisible(!visible)}>{visible ? 'Ẩn' : 'Hiện'}</button></div>
            {register && <><p id="password-hint" className="auth-hint">Từ 8–128 ký tự. Khoảng trắng được giữ nguyên.</p><label htmlFor="confirmPassword">Nhập lại mật khẩu<input id="confirmPassword" name="confirmPassword" type={visible ? 'text' : 'password'} autoComplete="new-password" required maxLength="128" disabled={busy} /></label></>}
            {error && <p className="auth-error" role="alert" tabIndex="-1" ref={alert}>{error}</p>}
            <button className="auth-submit" type="submit" disabled={busy}>{busy ? (register ? 'Đang tạo tài khoản…' : 'Đang đăng nhập…') : register ? 'Tạo tài khoản' : 'Đăng nhập'}</button>
          </form><p className="auth-switch">{register ? 'Đã có tài khoản?' : 'Chưa có tài khoản?'} <a href={(register ? '/login' : '/register') + '?next=' + encodeURIComponent(next)}>{register ? 'Đăng nhập' : 'Tạo tài khoản'}</a></p>
        </>}
      </div></section></main><footer className="auth-footer"><span>SHOPFLOW</span><a href="/catalog">Bộ sưu tập bàn phím</a></footer></div>;
}













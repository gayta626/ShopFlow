import { useSession } from '../lib/auth';
import { useEffect, useRef, useState } from 'react';
import KeyboardDrawing from './KeyboardDrawing';
import ProductCard from './ProductCard';

export function Icon({ name }) {
  const shapes = { search: <><circle cx="10.5" cy="10.5" r="6.5" /><path d="m16 16 5 5" /></>, user: <><circle cx="12" cy="8" r="4" /><path d="M4 22v-2a8 8 0 0 1 16 0v2" /></>, cart: <><path d="M3 3h2l3 13h11l3-9H6" /><circle cx="9" cy="21" r="1" /><circle cx="19" cy="21" r="1" /></>, menu: <path d="M3 6h18M3 12h18M3 18h18" /> };
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true">{shapes[name]}</svg>;
}

export default function Header({ categories, products, onFilter, onDetail, onPending, categoryStatus }) {
  const session = useSession();
  const [open, setOpen] = useState(false), [mobile, setMobile] = useState(false);
  const root = useRef(null), opener = useRef(null);
  useEffect(() => {
    function outside(event) { if (!root.current?.contains(event.target)) setOpen(false); }
    function escape(event) { if (event.key === 'Escape') { setOpen(false); setMobile(false); opener.current?.focus(); } }
    document.addEventListener('pointerdown', outside); document.addEventListener('keydown', escape);
    return () => { document.removeEventListener('pointerdown', outside); document.removeEventListener('keydown', escape); };
  }, []);
  function filter(values) { setOpen(false); setMobile(false); onFilter(values); }
  const menuProps = { 'aria-expanded': open, 'aria-controls': 'catalog-menu', onClick: event => { opener.current = event.currentTarget; setOpen(!open); }, onPointerEnter: event => { if (event.pointerType === 'mouse' && window.innerWidth > 1050) { opener.current = event.currentTarget; setOpen(true); } } };
  return <><div className="announcement">Chọn bàn phím cho góc làm việc của bạn <a href="/catalog">Khám phá bộ sưu tập ↗</a></div>
    <div className="header-wrap" ref={root} onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false); }} onPointerLeave={() => { if (window.innerWidth > 1050) setOpen(false); }}>
      <header className="header"><a href="/" className="brand" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt="" /><span>SHOP<span>FLOW</span><i /></span></a>
        <button className="menu-toggle icon-button" aria-label="Mở menu" aria-expanded={mobile} aria-controls="store-nav" onClick={() => { setMobile(!mobile); setOpen(false); }}><Icon name="menu" /></button>
        <nav id="store-nav" className={mobile ? 'main-nav mobile-open' : 'main-nav'} aria-label="Điều hướng cửa hàng"><button {...menuProps}>Tất cả sản phẩm</button><button {...menuProps}><span>Bàn phím</span><svg className="nav-chevron" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="m4 6 4 4 4-4" /></svg></button><button onClick={() => filter({})}>Mới về <i /></button><a href={session?.user.role==='Admin' ? '/admin/orders' : session ? '/account' : '/login?next=%2Faccount'}>{session?.user.role==='Admin'?'Quản lý đơn hàng':'Đơn của tôi'}</a></nav>
        <form className={`search ${mobile ? 'mobile-open' : ''}`} role="search" onSubmit={event => { event.preventDefault(); filter({ q: new FormData(event.currentTarget).get('q').trim() }); }}><input name="q" aria-label="Tìm bàn phím" placeholder="Tìm bàn phím, SKU…" maxLength="100" /><button className="icon-button" aria-label="Tìm kiếm"><Icon name="search" /></button></form>
        <div className="tools"><a className="icon-button" aria-label="Tài khoản" href={session ? '/account' : '/login'}><Icon name="user" /></a><a className="icon-button" aria-label="Giỏ hàng" href="/cart"><Icon name="cart" /></a></div>
      </header>
      <section className="mega" id="catalog-menu" aria-label="Danh mục bàn phím" hidden={!open}><div className="mega-inner"><div className="mega-heading"><h2>Tất cả bàn phím</h2><button className="close-menu" onClick={() => { setOpen(false); opener.current?.focus(); }}>Đóng ×</button></div><div className="browse-grid"><aside><h3>Bộ sưu tập</h3><button className="collection-all" onClick={() => filter({})}><KeyboardDrawing name="75%" />Tất cả sản phẩm</button><h3>Theo layout</h3><div className="layout-links">{categories.map(category => <button key={category.id} onClick={() => filter({ categoryId: category.id })}><KeyboardDrawing name={category.name} /><strong>{category.name}</strong></button>)}</div>{categoryStatus !== 'ready' && <p className="muted">{categoryStatus === 'error' ? 'Chưa tải được danh mục.' : 'Đang tải danh mục…'}</p>}<h3>Cảm giác gõ</h3><div className="switch-links">{['Linear', 'Tactile', 'Clicky'].map(value => <button key={value} onClick={() => filter({ switchType: value })}>{value}</button>)}</div></aside><div><div className="shortcuts">{[['Bluetooth','Không dây, gọn bàn'],['Wireless2_4G','Kết nối qua receiver'],['Wired','Cắm và sử dụng']].map(([value, label]) => <button key={value} onClick={() => filter({ connectivity: value })}>{value === 'Wired' ? 'Có dây' : value === 'Wireless2_4G' ? '2.4 GHz' : value}<span>{label}</span></button>)}</div><div className="menu-products">{products.map(p => <ProductCard key={p.id} product={p} onDetail={p => { setOpen(false); onDetail(p); }} />)}</div><button className="browse-all" onClick={() => filter({})}>Khám phá tất cả bàn phím <span>↗</span></button></div></div></div></section>
    </div></>;
}






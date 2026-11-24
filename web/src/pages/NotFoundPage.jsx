import { useEffect } from 'react';
import '../cart.css';

export default function NotFoundPage(){
  useEffect(()=>{document.title='Không tìm thấy trang | ShopFlow';},[]);
  return <><header className="cart-header"><a className="brand" href="/" aria-label="ShopFlow — Trang chủ"><img src="/favicon.svg" alt=""/><span>SHOP<span>FLOW</span><i/></span></a><a href="/account">Tài khoản</a></header><main className="cart-page" tabIndex="-1"><section className="cart-feedback"><span className="cart-eyebrow">SHOPFLOW / 404</span><h1>Không tìm thấy trang.</h1><p>Đường dẫn này không tồn tại. Bạn có thể quay về cửa hàng để tiếp tục khám phá.</p><a className="cart-primary" href="/catalog">Khám phá bàn phím</a><a href="/">Về trang chủ</a></section></main></>;
}

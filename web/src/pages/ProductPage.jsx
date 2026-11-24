import { navigate } from '../lib/navigation';
import { useEffect, useState } from 'react';
import Header from '../components/Header';
import ProductGallery from '../components/ProductGallery';
import ProductCard from '../components/ProductCard';
import QuickInfo from '../components/QuickInfo';
import AddToCart from '../components/AddToCart';
import '../cart.css';
import { getJson, money } from '../lib/api';
import '../product.css';

export default function ProductPage() {
  const id = window.location.pathname.match(/^\/products\/([1-9]\d*)\/?$/)?.[1];
  const [product, setProduct] = useState(null), [error, setError] = useState(null), [retry, setRetry] = useState(0);
  const [categories, setCategories] = useState([]), [categoryStatus, setCategoryStatus] = useState('loading');
  const [menuProducts, setMenuProducts] = useState([]), [related, setRelated] = useState([]), [relatedStatus, setRelatedStatus] = useState('loading');
  const [content, setContent] = useState(null);
  const go = values => { navigate(`/catalog?${new URLSearchParams(values)}`); };
  const pending = title => setContent({ title, message: 'Chức năng này sẽ được nối ở bước tài khoản và giỏ hàng. Chưa có sản phẩm nào được thêm vào giỏ.' });
  useEffect(() => {
    const controller = new AbortController();
    document.title = 'Chi tiết bàn phím | ShopFlow';
    setProduct(null); setError(null);
    if (!id || Number(id) > 2147483647) { setError({status:404}); return () => controller.abort(); }
    getJson(`products/${id}`, controller.signal).then(value => { setProduct(value); document.title = `${value.name} | ShopFlow`; }).catch(error => { if (error.name !== 'AbortError') setError(error); });
    return () => controller.abort();
  }, [id, retry]);
  useEffect(() => {
    const controller = new AbortController();
    setCategoryStatus('loading');
    getJson('categories', controller.signal).then(value => { setCategories(value); setCategoryStatus('ready'); }).catch(error => { if (error.name !== 'AbortError') setCategoryStatus('error'); });
    getJson('products?size=3&sort=newest', controller.signal).then(value => setMenuProducts(value.items)).catch(() => {});
    return () => controller.abort();
  }, [retry]);
  useEffect(() => {
    if (!product) return;
    const controller = new AbortController(); setRelatedStatus('loading'); setRelated([]);
    getJson(`products?categoryId=${product.categoryId}&size=4&sort=newest`, controller.signal).then(value => { setRelated(value.items.filter(item => item.id !== product.id).slice(0,3)); setRelatedStatus('ready'); }).catch(error => { if (error.name !== 'AbortError') setRelatedStatus('error'); });
    return () => controller.abort();
  }, [product]);
  const specs = product ? [['SKU',product.sku],['Thương hiệu',product.brand],['Layout',product.layout],['Switch',product.switchType],['Màu sắc',product.color],['Kết nối',product.connectivity.map(value => ({Wired:'Có dây',Bluetooth:'Bluetooth',Wireless2_4G:'2.4 GHz'})[value] || value).join(' / ')],['Hot swap',product.isHotSwappable === null ? 'Chưa có thông tin' : product.isHotSwappable ? 'Có' : 'Không']] : [];
  return <><a className="skip" href="#main">Đến nội dung chính</a><Header categories={categories} categoryStatus={categoryStatus} products={menuProducts} onFilter={go} onDetail={item => { navigate(`/products/${item.id}`); }} onPending={pending} />
    <main className="product-page" id="main" tabIndex="-1"><nav className="product-crumb" aria-label="Đường dẫn"><a href="/">Trang chủ</a><span>/</span><a href="/catalog">Bàn phím</a>{product && <><span>/</span><span aria-current="page">{product.name}</span></>}</nav>
      {error ? <section className="detail-feedback" role="alert"><h1>{error.status === 404 ? 'Không tìm thấy sản phẩm' : 'Chưa tải được sản phẩm'}</h1><p>{error.status === 404 ? 'Sản phẩm không tồn tại hoặc đã ngừng bán.' : error.message}</p>{error.status !== 404 && <button className="detail-primary" onClick={() => setRetry(value => value + 1)}>Thử lại</button>}<a href="/catalog">Xem tất cả bàn phím</a></section> : !product ? <section className="detail-loading" aria-busy="true"><p role="status">Đang tải chi tiết bàn phím…</p><div /></section> : <>
        <div className="detail-top"><ProductGallery /><section className="detail-summary"><span className="detail-eyebrow">SHOPFLOW / {product.layout}</span><h1>{product.name}</h1><p className="detail-brand">{product.brand} <span>·</span> {product.sku}</p><div className="detail-price"><strong>{money(product.price)}</strong><span className={product.stock > 0 ? 'available' : ''}>{product.stock > 0 ? `Còn ${product.stock} sản phẩm` : 'Tạm hết hàng'}</span></div><div className="detail-config"><span>Cấu hình sản phẩm</span><div>{[product.color,product.layout,product.switchType].map((value,index) => <b key={index}>{value}</b>)}</div><p>Mỗi cấu hình có SKU và lượng tồn riêng.</p></div>
          <AddToCart product={product} />
          <dl className="detail-quick-specs">{specs.filter(([label]) => ['Kết nối','Hot swap'].includes(label)).map(([label,value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl><a className="detail-text-link" href="#specifications">Xem đầy đủ thông số ↓</a>
        </section></div>
        <nav className="detail-tabs" aria-label="Nội dung sản phẩm"><a href="#description">Mô tả sản phẩm</a><a href="#specifications">Thông số kỹ thuật</a><a href="#related">Cùng layout</a></nav>
        <section className="detail-description" id="description"><div><span className="detail-eyebrow">VỀ CHIẾC BÀN PHÍM NÀY</span><h2>Mô tả sản phẩm.</h2><p>{product.description?.trim() || 'Cửa hàng chưa bổ sung mô tả cho sản phẩm này.'}</p></div><div className="detail-editorial"><img src="/assets/q1-keys.jpg" alt="Ảnh minh họa chi tiết phím, không phải ảnh của SKU này" loading="lazy" /><span>Ảnh minh họa</span></div></section>
        <section className="detail-specifications" id="specifications"><div><span className="detail-eyebrow">CẤU HÌNH ĐANG XEM</span><h2>Thông số kỹ thuật.</h2><p>Thông tin theo SKU {product.sku}.</p></div><dl>{specs.map(([label,value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl></section>
        <section className="detail-related" id="related"><div className="related-heading"><div><span className="detail-eyebrow">TIẾP TỤC KHÁM PHÁ</span><h2>Cùng layout {product.layout}.</h2></div><a href={`/catalog?categoryId=${product.categoryId}`}>Xem bộ sưu tập</a></div>{relatedStatus === 'loading' ? <p role="status">Đang tải sản phẩm cùng layout…</p> : relatedStatus === 'error' ? <p>Chưa tải được gợi ý. Bạn có thể <a href={`/catalog?categoryId=${product.categoryId}`}>xem bộ sưu tập</a>.</p> : related.length ? <div className="related-grid">{related.map(item => <ProductCard key={item.id} product={item} />)}</div> : <p>Chưa có sản phẩm khác trong layout này. <a href="/catalog">Khám phá tất cả bàn phím.</a></p>}</section>
      </>}
    </main><footer><a className="footer-brand" href="/">SHOPFLOW</a><span>Ảnh tham khảo · Thông số và giá theo cửa hàng</span><a href="#main">Về đầu trang ↑</a></footer><QuickInfo content={content} onClose={() => setContent(null)} /></>;
}


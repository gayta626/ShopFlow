import { navigate } from '../lib/navigation';
import { useEffect, useRef, useState } from 'react';
import Header from '../components/Header';
import ProductCard from '../components/ProductCard';
import AllProducts from '../components/AllProducts';
import KeyboardDrawing from '../components/KeyboardDrawing';
import { getJson, money } from '../lib/api';

export default function HomePage() {
  useEffect(() => { document.title = 'ShopFlow — Bàn phím cho góc làm việc'; }, []);
  const [categories, setCategories] = useState([]), [categoryStatus, setCategoryStatus] = useState('loading');
  const [products, setProducts] = useState([]), [newest, setNewest] = useState([]), [total, setTotal] = useState(0);
  const [status, setStatus] = useState('loading'), [error, setError] = useState(''), [filters, setFilters] = useState({}), [page, setPage] = useState(1), [retry, setRetry] = useState(0);
  const [dialogContent, setDialogContent] = useState(null), dialog = useRef(null);
  const size = 3;
  useEffect(() => {
    const controller = new AbortController();
    setCategoryStatus('loading');
    getJson('categories', controller.signal).then(data => { setCategories(data); setCategoryStatus('ready'); }).catch(error => { if (error.name !== 'AbortError') setCategoryStatus('error'); });
    return () => controller.abort();
  }, [retry]);
  useEffect(() => {
    const controller = new AbortController();
    setStatus('loading');
    const params = new URLSearchParams({ ...filters, page, size, sort: 'newest' });
    getJson(`products?${params}`, controller.signal).then(data => {
      setProducts(data.items); setTotal(data.totalCount); setStatus('ready');
      if (Object.keys(filters).length === 0 && page === 1) setNewest(data.items);
    }).catch(error => { if (error.name !== 'AbortError') { setError(error.message); setStatus('error'); } });
    return () => controller.abort();
  }, [filters, page, retry]);
  useEffect(() => { if (dialogContent) dialog.current?.showModal(); }, [dialogContent]);
  function filter(values) { navigate(`/catalog?${new URLSearchParams(values)}`); }
  const pending = name => setDialogContent({ title: name, message: 'Trang này sẽ được triển khai ở bước tiếp theo. Hiện bạn đang xem trang chủ React đầu tiên của ShopFlow.' });
  const detail = product => setDialogContent({ title: product.name, product });
  return <><a className="skip" href="#main">Đến nội dung chính</a><Header categories={categories} categoryStatus={categoryStatus} products={newest} onFilter={values => { navigate(`/catalog?${new URLSearchParams(values)}`); }} onDetail={detail} onPending={pending} />
    <main id="main" className="wrap storefront-home" tabIndex="-1">
      <section className="sf-home-hero" aria-label="Bộ sưu tập bàn phím"><img className="sf-hero-image" src="/assets/keychron-banner.jpg" alt="Bàn phím Keychron, ảnh tham khảo phong cách góc làm việc" fetchPriority="high" /><div className="sf-hero-shade" /><div className="sf-hero-copy"><span className="sf-home-kicker"><i /> Mới tại ShopFlow</span><h1>Một nhịp gõ mới.<br />Một góc riêng bạn.</h1><p>Bàn phím lắp sẵn, cấu hình rõ ràng.<br />Khám phá lựa chọn mới cho bàn làm việc.</p><a className="sf-home-cta" href="#products">Khám phá bộ sưu tập <span aria-hidden="true">↗</span></a></div><div className="sf-hero-caption"><span>FIND YOUR EVERYDAY KEYBOARD</span><span>Ảnh Keychron · Tham khảo phong cách</span></div></section>
      <section className="sf-home-layouts sf-home-contained" aria-labelledby="layout-title"><div className="sf-home-section-top"><div><span className="sf-home-kicker">Tìm đúng kích thước</span><h2 id="layout-title">Bàn của bạn. Layout của bạn.</h2></div><button className="sf-home-textlink" onClick={() => filter({})}>Tất cả bàn phím <span>↗</span></button></div>
        {categoryStatus === 'loading' ? <p role="status">Đang tải danh mục…</p> : categoryStatus === 'error' ? <div className="feedback" role="alert"><p>Chưa tải được danh mục.</p><button onClick={() => setRetry(retry + 1)}>Thử lại</button></div> : categories.length === 0 ? <p>Cửa hàng chưa có danh mục.</p> : <div className="sf-layout-grid">{categories.map(category => <button className="sf-layout-item" key={category.id} onClick={() => filter({ categoryId: category.id })}><div className="sf-layout-photo"><KeyboardDrawing name={category.name} /><span>{category.name}</span></div><h3>Khám phá {category.name}</h3><p>Xem bàn phím thuộc bộ sưu tập này.</p></button>)}</div>}
      </section>
      <section id="products" className="sf-home-featured sf-home-contained" aria-labelledby="products-title"><div className="sf-home-section-top"><div><span className="sf-home-kicker">Trong cửa hàng</span><h2 id="products-title">{Object.keys(filters).length ? 'Bàn phím bạn đang tìm.' : 'Bàn phím mới nhất.'}</h2></div>{Object.keys(filters).length > 0 && <button className="sf-home-textlink" onClick={() => filter({})}>Xóa bộ lọc ×</button>}</div>
        <div className="results-summary" role="status">{status === 'ready' ? `${total} sản phẩm${filters.q ? ` · Từ khóa: ${filters.q}` : ''}` : status === 'loading' ? 'Đang tải sản phẩm…' : ''}</div>
        {status === 'loading' ? <div className="sf-home-product-grid" aria-busy="true">{[1,2,3].map(n => <div className="skeleton" key={n} />)}</div> : status === 'error' ? <div className="feedback" role="alert"><h3>Chưa thể tải sản phẩm</h3><p>{error}</p><button className="primary" onClick={() => setRetry(retry + 1)}>Thử lại</button></div> : products.length ? <><div className="sf-home-product-grid">{products.map(p => <ProductCard key={p.id} product={p} onDetail={detail} />)}</div>{total > size && <nav className="pagination" aria-label="Phân trang sản phẩm"><button disabled={page === 1} onClick={() => setPage(page - 1)}>Trang trước</button><span>Trang {page} / {Math.ceil(total / size)}</span><button disabled={page * size >= total} onClick={() => setPage(page + 1)}>Trang sau</button></nav>}</> : <div className="feedback"><h3>Chưa có bàn phím phù hợp</h3><p>Thử một layout khác hoặc xóa bộ lọc để xem lại cửa hàng.</p><button onClick={() => filter({})}>Xem tất cả</button></div>}
      </section>
      <section className="sf-home-story"><div className="sf-story-photo"><img src="/assets/q1-black.jpg" alt="Góc bàn với bàn phím Keychron, ảnh tham khảo" loading="lazy" /><span>THE DETAILS MAKE THE DIFFERENCE.</span></div><div className="sf-story-copy"><span className="sf-home-kicker">Nhìn gần hơn một chút</span><h2>Chọn bằng mắt.<br />Hiểu từng chi tiết.</h2><p>Một chiếc bàn phím không chỉ nằm ở màu sắc. Tìm layout và cảm giác gõ hợp với cách bạn làm việc.</p><div className="sf-story-points">{[['Switch theo tay gõ','Linear mượt, tactile có khấc, clicky có tiếng phản hồi.'],['Kết nối theo góc bàn','Kiểm tra cấu hình có dây hoặc không dây của từng sản phẩm.'],['Cấu hình rõ ràng','Chọn theo layout, màu sắc và khả năng hot swap.']].map(([title, text], i) => <div key={title}><span>0{i+1}</span><p><strong>{title}</strong>{text}</p></div>)}</div><a className="sf-home-textlink" href="#products">Khám phá bộ sưu tập <span>↗</span></a></div></section>
      <AllProducts onDetail={detail} />
      <section className="sf-home-end sf-home-contained"><span className="sf-home-kicker">Sẵn sàng cho góc làm việc mới?</span><h2>Bắt đầu từ chiếc bàn phím<br />bạn muốn chạm vào mỗi ngày.</h2><a className="sf-home-cta" href="#products">Tìm bàn phím của bạn <span>↗</span></a></section>
    </main><footer><a className="footer-brand" href="/">SHOPFLOW</a><span>Giá và cấu hình từ cửa hàng · Ảnh banner tham khảo Keychron</span><a href="#main">Về đầu trang ↑</a></footer>
    <dialog ref={dialog} onClose={() => setDialogContent(null)} onClick={event => { if (event.target === dialog.current) dialog.current.close(); }} aria-labelledby="dialog-title"><button className="dialog-close" onClick={() => dialog.current.close()} aria-label="Đóng">×</button><h2 id="dialog-title">{dialogContent?.title}</h2>{dialogContent?.product ? <><p className="muted">Thông tin nhanh · Trang chi tiết sẽ được làm ở bước tiếp theo.</p><dl>{[['SKU',dialogContent.product.sku],['Layout',dialogContent.product.layout],['Switch',dialogContent.product.switchType],['Giá',money(dialogContent.product.price)],['Tồn kho',dialogContent.product.stock]].map(([k,v]) => <div key={k}><dt>{k}</dt><dd>{v}</dd></div>)}</dl></> : <p>{dialogContent?.message}</p>}</dialog>
  </>;
}


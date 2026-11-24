import { navigate } from '../lib/navigation';
import { useEffect, useState } from 'react';
import Header from '../components/Header';
import ProductCard from '../components/ProductCard';
import QuickInfo from '../components/QuickInfo';
import { getJson } from '../lib/api';
import '../catalog.css';

const fields = ['q','categoryId','switchType','connectivity','minPrice','maxPrice','sort','page','size'];

export default function CatalogPage() {
  const [query] = useState(() => {
    const input = new URLSearchParams(window.location.search);
    const result = new URLSearchParams();
    fields.forEach(key => { if (input.get(key)) result.set(key, input.get(key)); });
    if (!result.has('sort')) result.set('sort', 'newest');
    if (!result.has('size')) result.set('size', '12');
    if (!result.has('page')) result.set('page', '1');
    return result;
  });
  const [categories, setCategories] = useState([]), [categoryStatus, setCategoryStatus] = useState('loading');
  const [data, setData] = useState(null), [error, setError] = useState(''), [retry, setRetry] = useState(0);
  const [formError, setFormError] = useState(''), [content, setContent] = useState(null);
  const [menuProducts, setMenuProducts] = useState([]);
  useEffect(() => {
    document.title = 'Bộ sưu tập bàn phím | ShopFlow';
    const controller = new AbortController();
    setCategoryStatus('loading');
    getJson('categories', controller.signal).then(value => { setCategories(value); setCategoryStatus('ready'); }).catch(error => { if (error.name !== 'AbortError') setCategoryStatus('error'); });
    getJson('products?size=3&sort=newest', controller.signal).then(set => setMenuProducts(set.items)).catch(() => {});
    return () => controller.abort();
  }, [retry]);
  useEffect(() => {
    const controller = new AbortController();
    setData(null); setError('');
    getJson(`products?${query}`, controller.signal).then(setData).catch(error => { if (error.name !== 'AbortError') setError(error.message); });
    return () => controller.abort();
  }, [query, retry]);
  const go = values => { navigate(`/catalog?${new URLSearchParams(values)}`); };
  const detail = product => setContent({title:product.name, product});
  const pending = title => setContent({title, message:'Trang này chưa triển khai. Bạn có thể tiếp tục xem bộ sưu tập bàn phím.'});
  function submit(event) {
    event.preventDefault();
    const values = new FormData(event.currentTarget);
    if (values.get('minPrice') && values.get('maxPrice') && Number(values.get('minPrice')) > Number(values.get('maxPrice'))) {
      setFormError('Giá từ không được lớn hơn giá đến.');
      event.currentTarget.elements.minPrice.focus(); return;
    }
    const params = new URLSearchParams();
    values.forEach((value, key) => { if (String(value).trim()) params.set(key, String(value).trim()); });
    go(params);
  }
  function pageLink(page) { const params = new URLSearchParams(query); params.set('page',page); return `/catalog?${params}`; }
  const active = ['q','categoryId','switchType','connectivity','minPrice','maxPrice'].filter(key => query.has(key));
  const pages = data ? Math.ceil(data.totalCount / data.size) : 0;
  return <><a className="skip" href="#main">Đến nội dung chính</a><Header categories={categories} categoryStatus={categoryStatus} products={menuProducts} onFilter={go} onDetail={detail} onPending={pending} />
    <main className="catalog-page" id="main" tabIndex="-1"><div className="catalog-crumb"><a href="/">Trang chủ</a><span>/</span><span>Bàn phím</span></div>
      <section className="catalog-intro"><div><span className="catalog-kicker">SHOPFLOW / BỘ SƯU TẬP</span><h1>Tìm chiếc bàn phím<br />hợp với bạn.</h1><p>Từ layout đến cảm giác gõ.<br />Chọn cấu hình cho góc làm việc của riêng mình.</p></div><img src="/assets/keyboard.jpg" alt="Bàn phím tham khảo cho bộ sưu tập" /><span className="catalog-photo-note">Ảnh minh họa</span></section>
      <div className="catalog-layout"><aside className="catalog-filters"><div className="filter-heading"><h2>Bộ lọc</h2><a href="/catalog">Đặt lại</a></div><form onSubmit={submit} id="catalog-form">
        <label>Tên hoặc SKU<input name="q" defaultValue={query.get('q') || ''} maxLength="100" placeholder="Tìm chiếc bàn phím…" /></label>
        <label>Layout<select name="categoryId" key={categoryStatus} defaultValue={query.get('categoryId') || ''}><option value="">Tất cả layout</option>{categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
        {categoryStatus === 'error' && <div role="alert" className="filter-warning">Chưa tải được layout. <button type="button" onClick={() => setRetry(retry + 1)}>Thử lại danh mục</button></div>}
        <label>Loại switch<select name="switchType" defaultValue={query.get('switchType') || ''}><option value="">Tất cả switch</option>{['Linear','Tactile','Clicky'].map(value => <option key={value}>{value}</option>)}</select></label>
        <label>Kết nối<select name="connectivity" defaultValue={query.get('connectivity') || ''}><option value="">Tất cả kết nối</option><option value="Wired">Có dây</option><option value="Bluetooth">Bluetooth</option><option value="Wireless2_4G">2.4 GHz</option></select></label>
        <fieldset><legend>Khoảng giá (₫)</legend><div className="price-inputs"><label>Giá từ<input name="minPrice" type="number" min="0" step="any" defaultValue={query.get('minPrice') || ''} placeholder="0" aria-describedby={formError ? 'price-error' : undefined} /></label><label>Giá đến<input name="maxPrice" type="number" min="0" step="any" defaultValue={query.get('maxPrice') || ''} placeholder="Không giới hạn" /></label></div></fieldset>
        {formError && <p id="price-error" className="filter-warning" role="alert">{formError}</p>}
        <button className="filter-submit" type="submit">Áp dụng bộ lọc</button>
      </form><p className="filter-note">Giá và tồn kho lấy từ cửa hàng. Ảnh đang dùng để minh họa, chưa đại diện cho từng SKU.</p></aside>
      <section className="catalog-results" id="products" aria-labelledby="catalog-results-title"><div className="catalog-toolbar"><div><h2 id="catalog-results-title">{active.length ? 'Kết quả của bạn' : 'Tất cả bàn phím'}</h2><p role="status">{data ? `${data.totalCount} sản phẩm` : error ? 'Chưa tải được dữ liệu' : 'Đang tìm bàn phím…'}</p></div><div className="catalog-sort"><label>Sắp xếp<select name="sort" form="catalog-form" defaultValue={query.get('sort')} onChange={event => event.target.form.requestSubmit()}><option value="newest">Mới nhất</option><option value="priceAsc">Giá tăng dần</option><option value="priceDesc">Giá giảm dần</option><option value="nameAsc">Tên A–Z</option></select></label><label>Hiển thị<select name="size" form="catalog-form" defaultValue={query.get('size')} onChange={event => event.target.form.requestSubmit()}>{[2,12,24,48].map(size => <option key={size} value={size}>{size} / trang</option>)}</select></label></div></div>
        {active.length > 0 && <div className="active-filters">{active.map(key => { const label = key === 'categoryId' ? categories.find(c=>String(c.id)===query.get(key))?.name || 'Layout đã chọn' : key === 'minPrice' ? `Từ ${query.get(key)} ₫` : key === 'maxPrice' ? `Đến ${query.get(key)} ₫` : query.get(key); const params = new URLSearchParams(query); params.delete(key); params.delete('page'); return <a key={key} href={`/catalog?${params}`} aria-label={`Bỏ lọc ${label}`}>{label}<span aria-hidden="true">×</span></a>; })}<a href="/catalog">Xóa tất cả</a></div>}
        {error ? <div className="feedback" role="alert"><h3>Chưa thể tải sản phẩm</h3><p>{error}</p><button onClick={() => setRetry(retry + 1)}>Thử lại</button><a className="reset-catalog" href="/catalog">Đặt lại bộ lọc</a></div> : !data ? <div className="catalog-grid" aria-busy="true">{[1,2,3].map(value=><div className="skeleton" key={value} />)}</div> : data.items.length ? <><div className="catalog-grid">{data.items.map(product => <ProductCard key={product.id} product={product} onDetail={detail} />)}</div><div className="catalog-pagination"><span>Đang xem {(data.page-1)*data.size+1}–{Math.min(data.page*data.size,data.totalCount)} / {data.totalCount}</span>{pages > 1 && <nav aria-label="Phân trang sản phẩm">{data.page > 1 && <a href={pageLink(data.page-1)}>Trang trước</a>}<span>Trang {data.page} / {pages}</span>{data.page < pages && <a href={pageLink(data.page+1)}>Trang sau</a>}</nav>}</div></> : <div className="feedback"><h3>{data.totalCount ? 'Trang này không có sản phẩm' : 'Chưa có bàn phím phù hợp'}</h3><p>{data.totalCount ? 'Quay về trang đầu để xem các sản phẩm phù hợp.' : 'Thử một layout khác hoặc mở rộng khoảng giá.'}</p><a className="reset-catalog" href={data.totalCount ? pageLink(1) : '/catalog'}>{data.totalCount ? 'Về trang đầu' : 'Xóa bộ lọc'}</a></div>}
      </section></div>
    </main><footer><a className="footer-brand" href="/">SHOPFLOW</a><span>Ảnh tham khảo từ bộ prototype · Thông số và giá theo API ShopFlow</span><a href="#main">Về đầu trang ↑</a></footer><QuickInfo content={content} onClose={() => setContent(null)} />
  </>;
}


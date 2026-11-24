import { useEffect, useRef, useState } from 'react';
import { getJson } from '../lib/api';
import ProductCard from './ProductCard';

export default function AllProducts({ onDetail }) {
  const [data, setData] = useState(null), [error, setError] = useState(false);
  const [page, setPage] = useState(1), [retry, setRetry] = useState(0);
  const rail = useRef(null);
  useEffect(() => {
    const controller = new AbortController();
    setData(null); setError(false);
    getJson(`products?page=${page}&size=10&sort=newest`, controller.signal)
      .then(value => { setData(value); if (rail.current) rail.current.scrollLeft = 0; })
      .catch(error => { if (error.name !== 'AbortError') setError(true); });
    return () => controller.abort();
  }, [page, retry]);
  return <section className="all-products sf-home-contained" aria-labelledby="all-products-title">
    <div className="sf-home-section-top"><div><span className="sf-home-kicker">Khám phá cửa hàng</span><h2 id="all-products-title">Tất cả sản phẩm</h2></div><div className="rail-controls"><a href="/catalog">Xem tất cả</a><button aria-label="Cuộn sản phẩm sang trái" onClick={() => rail.current?.scrollBy({ left: -360, behavior: 'auto' })}>←</button><button aria-label="Cuộn sản phẩm sang phải" onClick={() => rail.current?.scrollBy({ left: 360, behavior: 'auto' })}>→</button></div></div>
    {error ? <div className="feedback" role="alert"><p>Chưa tải được bộ sưu tập.</p><button onClick={() => setRetry(retry + 1)}>Tải lại bộ sưu tập</button></div> : !data ? <p role="status">Đang tải bộ sưu tập…</p> : data.items.length === 0 ? <p>Cửa hàng chưa có sản phẩm.</p> : <><div className="product-rail" ref={rail} tabIndex="0" aria-label="Danh sách tất cả sản phẩm, cuộn ngang để xem thêm">{data.items.map(product => <ProductCard key={product.id} product={product} onDetail={onDetail} />)}</div><div className="rail-footer"><span>{data.totalCount} sản phẩm trong cửa hàng</span>{data.totalCount > 10 && <nav className="pagination" aria-label="Trang bộ sưu tập"><button disabled={page === 1} onClick={() => setPage(page - 1)}>Trang trước</button><span>{page} / {Math.ceil(data.totalCount / 10)}</span><button disabled={page * 10 >= data.totalCount} onClick={() => setPage(page + 1)}>Trang sau</button></nav>}</div></>}
  </section>;
}

import { money } from '../lib/api';
import { productImage } from '../lib/productImages';

export default function ProductCard({ product }) {
  return <article className="product-card">
    <a className="product-open" href={`/products/${product.id}`} aria-label={`Xem ${product.name}`}>
      <div className="product-visual"><img src={productImage(product)} alt={`Ảnh bàn phím tham khảo cho ${product.name}, chưa phải ảnh SKU thực tế`} loading="lazy" /><span>Ảnh minh họa</span></div>
      <div className="card-body"><span className="muted">{product.layout} · {product.switchType}</span><h3>{product.name}</h3><div className="product-spec-chips"><span>{product.connectivity.map(value => ({ Wired: 'Có dây', Bluetooth: 'Bluetooth', Wireless2_4G: '2.4 GHz' })[value] || value).join(' / ')}</span>{product.isHotSwappable === true && <span>Hot swap</span>}</div><span className="stock">{product.stock > 0 ? 'Còn hàng' : 'Tạm hết hàng'}</span><div className="card-bottom"><strong>{money(product.price)}</strong><span>Xem thông tin ↗</span></div></div>
    </a>
  </article>;
}

import { useEffect, useRef } from 'react';
import { money } from '../lib/api';
import { productImage } from '../lib/productImages';

export default function QuickInfo({ content, onClose }) {
  const dialog = useRef(null);
  useEffect(() => { if (content) dialog.current.showModal(); }, [content]);
  return <dialog ref={dialog} onClose={onClose} onClick={event => { if (event.target === dialog.current) dialog.current.close(); }} aria-labelledby="quick-title">
    <button className="dialog-close" onClick={() => dialog.current.close()} aria-label="Đóng">×</button><h2 id="quick-title">{content?.title}</h2>
    {content?.product ? <><img className="quick-photo" src={productImage(content.product)} alt="Ảnh bàn phím minh họa, không phải ảnh SKU thực tế" /><p className="muted">Ảnh minh họa · Trang chi tiết sẽ được triển khai tiếp.</p><dl>{[['SKU',content.product.sku],['Layout',content.product.layout],['Switch',content.product.switchType],['Màu sắc',content.product.color],['Giá',money(content.product.price)],['Tồn kho',content.product.stock]].map(([key,value]) => <div key={key}><dt>{key}</dt><dd>{value}</dd></div>)}</dl></> : <p>{content?.message}</p>}
  </dialog>;
}

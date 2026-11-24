import { useRef, useState } from 'react';

const photos = [
  { src: '/assets/angle-1.jpg', label: 'Góc bàn' },
  { src: '/assets/q1-black.jpg', label: 'Toàn cảnh' },
  { src: '/assets/q1-keys.jpg', label: 'Chi tiết phím' },
  { src: '/assets/keyboard.jpg', label: 'Góc nghiêng' },
];

export default function ProductGallery() {
  const [active, setActive] = useState(0), [instant, setInstant] = useState(false);
  const box = useRef(null), touch = useRef(null);
  const change = (next, keyboard = false) => { setInstant(keyboard); setActive((next + photos.length) % photos.length); };
  const keys = event => {
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault(); change(active + (event.key === 'ArrowRight' ? 1 : -1), true);
    }
  };
  return <section className="detail-gallery" aria-label="Gallery ảnh minh họa" id="gallery">
    <div className="gallery-label"><span>KHÁM PHÁ TỪNG GÓC NHÌN</span><span aria-live="polite">0{active + 1} / 04</span></div>
    <div className={`gallery-stage ${instant ? 'instant' : ''}`} tabIndex="0" role="group" aria-label="Dùng phím mũi tên để đổi ảnh" onKeyDown={keys}
      onTouchStart={event => { touch.current = { x: event.touches[0].clientX, y: event.touches[0].clientY }; }}
      onTouchCancel={() => { touch.current = null; }}
      onTouchEnd={event => { if (!touch.current) return; const dx = event.changedTouches[0].clientX - touch.current.x, dy = event.changedTouches[0].clientY - touch.current.y; if (Math.abs(dx) > 45 && Math.abs(dx) > Math.abs(dy)) change(active + (dx < 0 ? 1 : -1)); touch.current = null; }}>
      {photos.map((photo, index) => { const position = (index - active + photos.length) % photos.length; return <button key={photo.src} className="gallery-layer" data-slot={position === 3 ? -1 : position} tabIndex={index === active ? 0 : -1} aria-hidden={index !== active} aria-label={`Phóng to ảnh ${photo.label}`} onClick={() => index === active ? box.current.showModal() : change(index)}>
        <img src={photo.src} alt={`Ảnh bàn phím minh họa — ${photo.label}, không phải ảnh SKU thực tế`} draggable="false" />
      </button>; })}
    </div>
    <div className="gallery-controls"><button aria-label="Ảnh trước" onClick={event => change(active - 1, event.detail === 0)}>←</button><strong aria-live="polite">{photos[active].label}</strong><button aria-label="Ảnh tiếp theo" onClick={event => change(active + 1, event.detail === 0)}>→</button><button aria-label="Phóng to ảnh" onClick={() => box.current.showModal()}>⤢</button></div>
    <div className="gallery-thumbs">{photos.map((photo, index) => <button key={photo.src} aria-label={`Xem ảnh ${photo.label}`} aria-pressed={index === active} onClick={event => change(index, event.detail === 0)}><img src={photo.src} alt="" /><span>{photo.label}</span></button>)}</div>
    <p className="gallery-note">Bộ ảnh tham khảo · Không đại diện cho màu sắc hoặc cấu hình của SKU này.</p>
    <dialog className="product-lightbox" ref={box} aria-label="Ảnh minh họa phóng to" onKeyDown={keys} onClick={event => { if (event.target === box.current) box.current.close(); }}>
      <button className="lightbox-close" aria-label="Đóng ảnh phóng to" onClick={() => box.current.close()}>×</button>
      <img src={photos[active].src} alt={`Ảnh minh họa — ${photos[active].label}`} />
      <div><button aria-label="Ảnh trước trong phóng to" onClick={() => change(active - 1)}>←</button><span aria-live="polite">{photos[active].label} · {active + 1}/4</span><button aria-label="Ảnh tiếp theo trong phóng to" onClick={() => change(active + 1)}>→</button></div>
    </dialog>
  </section>;
}

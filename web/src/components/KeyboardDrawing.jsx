export default function KeyboardDrawing({ name = '' }) {
  const normalized = name.toLowerCase();
  const full = normalized.includes('full') || normalized.includes('100');
  const compact = normalized.includes('65');
  const columns = full ? 20 : 16;
  return <svg viewBox="0 0 300 150" role="img" aria-label={`Sơ đồ layout ${name}, không phải ảnh sản phẩm`}>
    <rect x="14" y="26" width="272" height="98" rx="10" fill="#302b28" />
    {Array.from({ length: compact ? 5 : 6 }, (_, row) => Array.from({ length: columns }, (_, col) => <rect key={`${row}-${col}`} x={21 + col * (258 / columns)} y={33 + row * (84 / (compact ? 5 : 6))} width={258 / columns - 3} height={84 / (compact ? 5 : 6) - 4} rx="2" fill={row === 0 && col === 0 ? '#f5ab7a' : row === 5 ? '#a99e94' : '#6c645d'} />))}
    <rect x="81" y={compact ? 101 : 103} width="88" height="10" rx="2" fill="#b9ada3" />
  </svg>;
}

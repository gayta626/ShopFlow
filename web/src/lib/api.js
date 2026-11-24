export async function getJson(path, signal) {
  const response = await fetch(`/api/${path}`, { signal, headers: { Accept: 'application/json' } });
  if (!response.ok) {
    const error = new Error(response.status === 400 ? 'Bộ lọc chưa hợp lệ. Bạn hãy thử lại.' : 'Chưa tải được dữ liệu cửa hàng. Vui lòng thử lại.');
    error.status = response.status;
    throw error;
  }
  return response.json();
}

export const money = value => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value);

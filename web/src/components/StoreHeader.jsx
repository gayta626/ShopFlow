import { useEffect, useState } from 'react';
import Header from './Header';
import { getJson } from '../lib/api';
import { navigate } from '../lib/navigation';

export default function StoreHeader() {
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [categoryStatus, setCategoryStatus] = useState('loading');
  useEffect(() => {
    const controller = new AbortController();
    getJson('categories', controller.signal).then(value => {
      setCategories(value); setCategoryStatus('ready');
    }).catch(error => { if (error.name !== 'AbortError') setCategoryStatus('error'); });
    getJson('products?size=3&sort=newest', controller.signal)
      .then(value => setProducts(value.items)).catch(() => {});
    return () => controller.abort();
  }, []);
  return <><a className="skip" href="#main">Đến nội dung chính</a><Header
    categories={categories} products={products} categoryStatus={categoryStatus}
    onFilter={values => navigate(`/catalog?${new URLSearchParams(values)}`)}
    onDetail={product => navigate(`/products/${product.id}`)}
  /></>;
}

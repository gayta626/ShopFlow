import AdminCategoriesPage from './pages/AdminCategoriesPage';
import NotFoundPage from './pages/NotFoundPage';
import AdminProductsPage from './pages/AdminProductsPage';
import AdminOrdersPage from './pages/AdminOrdersPage';
import OrderPage from './pages/OrderPage';
import CheckoutPage from './pages/CheckoutPage';
import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import HomePage from './pages/HomePage';
import CatalogPage from './pages/CatalogPage';
import ProductPage from './pages/ProductPage';
import AuthPage from './pages/AuthPage';
import CartPage from './pages/CartPage';
import { navigate } from './lib/navigation';
import './styles.css';
import './home.css';

function App() {
  const [route, setRoute] = useState(() => location.pathname + location.search);
  useEffect(() => {
    const changed = () => setRoute(location.pathname + location.search);
    const clicked = event => {
      const link = event.target.closest('a[href]');
      if (!link || event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || link.target || link.hasAttribute('download')) return;
      const url = new URL(link.href);
      if (url.origin !== location.origin || (url.pathname === location.pathname && url.search === location.search && url.hash)) return;
      event.preventDefault(); navigate(url.href);
    };
    window.addEventListener('popstate', changed); document.addEventListener('click', clicked);
    return () => { window.removeEventListener('popstate', changed); document.removeEventListener('click', clicked); };
  }, []);
  useEffect(() => { document.querySelector('main')?.focus({preventScroll:true}); }, [route]);
  const pathname = route.split('?')[0].replace(/\/$/, '');
  const Page = pathname === '/admin/categories' ? AdminCategoriesPage : /^\/admin\/products(?:\/[^/]+)?$/.test(pathname) ? AdminProductsPage : /^\/admin\/orders(?:\/[^/]+)?$/.test(pathname) ? AdminOrdersPage : /^\/orders\/[^/]+$/.test(pathname) ? OrderPage : pathname === '/checkout' ? CheckoutPage : pathname === '/cart' ? CartPage : ['/login','/register','/account','/welcome'].includes(pathname) ? AuthPage : /^\/products\/[^/]+$/.test(pathname) ? ProductPage : pathname === '/catalog' ? CatalogPage : pathname === '' ? HomePage : NotFoundPage;
  return <Page key={route} />;
}
createRoot(document.getElementById('root')).render(<React.StrictMode><App /></React.StrictMode>);







export function navigate(target, replace = false) {
  const url = new URL(target, window.location.origin);
  if (url.origin !== window.location.origin) return;
  window.history[replace ? 'replaceState' : 'pushState']({}, '', url.pathname + url.search + url.hash);
  window.dispatchEvent(new PopStateEvent('popstate'));
  window.scrollTo({ top: 0, behavior: 'instant' });
}

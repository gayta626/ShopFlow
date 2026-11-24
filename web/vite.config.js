import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  // The repository's C# path confuses Vite's HTML dependency scanner on Windows.
  optimizeDeps: { noDiscovery: true, include: ['react', 'react-dom/client', 'react/jsx-runtime', 'react/jsx-dev-runtime'] },
  server: { proxy: { '/api': { target: 'http://127.0.0.1:8080', changeOrigin: true } } },
  preview: { proxy: { '/api': { target: 'http://127.0.0.1:8080', changeOrigin: true } } },
});

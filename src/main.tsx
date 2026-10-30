import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App.tsx';
import './index.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);

// Makes the site installable as an app (see public/sw.js). Production only, so the Vite dev
// server is never served through a service worker.
if ('serviceWorker' in navigator && import.meta.env.PROD) {
  navigator.serviceWorker.register('/sw.js').catch(() => {
    /* not installable here (e.g. plain http) — the site works the same without it */
  });
}

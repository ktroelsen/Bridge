// Minimal service worker: it makes the site installable as an app. It caches nothing on
// purpose — every move goes to the API anyway, and a cache could serve a stale app after
// a deploy.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));
self.addEventListener('fetch', () => {
  // No respondWith: requests go straight to the network.
});

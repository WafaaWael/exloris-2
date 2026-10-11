// Network-first service worker.
// The stock Unity PWA worker is cache-first and never expires anything, so after a new build
// players keep getting the old index.html and the old Build files until they clear site data.
// This version always tries the network and only falls back to the cache when offline, so a
// new deployment is picked up on the next page load while offline play still works.
#if USE_DATA_CACHING
const cacheName = {{{JSON.stringify(COMPANY_NAME + "-" + PRODUCT_NAME + "-" + PRODUCT_VERSION )}}};
const contentToCache = [
    "./",
    "Build/{{{ LOADER_FILENAME }}}",
    "Build/{{{ FRAMEWORK_FILENAME }}}",
    "Build/{{{ DATA_FILENAME }}}",
    "Build/{{{ CODE_FILENAME }}}",
    "TemplateData/style.css"
];
#endif

self.addEventListener('install', function (e) {
    console.log('[Service Worker] Install');
    // Take over from any older worker immediately instead of waiting for all tabs to close.
    self.skipWaiting();
#if USE_DATA_CACHING
    e.waitUntil((async function () {
      try {
        const cache = await caches.open(cacheName);
        await cache.addAll(contentToCache);
      } catch (err) {
        // Pre-caching is best effort; the fetch handler fills the cache as files are used.
        console.warn('[Service Worker] Pre-cache failed', err);
      }
    })());
#endif
});

self.addEventListener('activate', function (e) {
    e.waitUntil((async function () {
#if USE_DATA_CACHING
      // Drop caches left behind by previous versions.
      const keys = await caches.keys();
      await Promise.all(keys.filter(function (k) { return k !== cacheName; }).map(function (k) { return caches.delete(k); }));
#endif
      await self.clients.claim();
    })());
});

#if USE_DATA_CACHING
self.addEventListener('fetch', function (e) {
    if (e.request.method !== 'GET') { return; }
    e.respondWith((async function () {
      const cache = await caches.open(cacheName);
      try {
        const response = await fetch(e.request);
        if (response && response.ok) {
          cache.put(e.request, response.clone());
        }
        return response;
      } catch (err) {
        const cached = await cache.match(e.request);
        if (cached) { return cached; }
        throw err;
      }
    })());
});
#endif

const CACHE_NAME = 'nettutos-v1';
const STATIC_ASSETS = [
    '/',
    '/offline.html',
    '/images/csharp-logo.png',
    '/css/site.css',
    '/js/site.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js'
];

// Install: Precache offline page and essential assets
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(STATIC_ASSETS);
        }).then(() => self.skipWaiting())
    );
});

// Activate: Clean up old caches
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(name => {
                    if (name !== CACHE_NAME) {
                        return caches.delete(name);
                    }
                })
            );
        }).then(() => self.clients.claim())
    );
});

// Fetch: Network-first for navigation, Cache-first for static assets
self.addEventListener('fetch', event => {
    const request = event.request;

    // Only handle GET requests
    if (request.method !== 'GET') return;

    // Navigation request (HTML pages)
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request)
                .then(response => {
                    // Cache successful page response dynamically
                    if (response.status === 200) {
                        const copy = response.clone();
                        caches.open(CACHE_NAME).then(cache => {
                            cache.put(request, copy);
                        });
                    }
                    return response;
                })
                .catch(async () => {
                    // Try to match cached page
                    const cachedResponse = await caches.match(request);
                    if (cachedResponse) {
                        return cachedResponse;
                    }
                    // Fallback to offline page
                    return caches.match('/offline.html');
                })
        );
        return;
    }

    // Static assets (CSS, JS, Fonts, Images)
    event.respondWith(
        caches.match(request).then(cachedResponse => {
            if (cachedResponse) {
                // Fetch in background to update cache (stale-while-revalidate)
                fetch(request).then(networkResponse => {
                    if (networkResponse && networkResponse.status === 200) {
                        caches.open(CACHE_NAME).then(cache => cache.put(request, networkResponse));
                    }
                }).catch(() => {});
                return cachedResponse;
            }

            return fetch(request).then(response => {
                if (response && response.status === 200 && (
                    request.url.includes('/css/') ||
                    request.url.includes('/js/') ||
                    request.url.includes('/images/') ||
                    request.url.includes('fonts.googleapis.com') ||
                    request.url.includes('cdn.jsdelivr.net')
                )) {
                    const copy = response.clone();
                    caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
                }
                return response;
            });
        })
    );
});

// Message handling for offline lesson management
self.addEventListener('message', async event => {
    const data = event.data;
    if (!data || !data.type) return;

    const cache = await caches.open(CACHE_NAME);

    if (data.type === 'SAVE_LESSON_OFFLINE') {
        try {
            const response = await fetch(data.url);
            if (response.ok) {
                await cache.put(data.url, response);
                event.source.postMessage({ type: 'LESSON_SAVED_SUCCESS', url: data.url });
            } else {
                event.source.postMessage({ type: 'LESSON_SAVED_FAILED', url: data.url });
            }
        } catch (e) {
            event.source.postMessage({ type: 'LESSON_SAVED_FAILED', url: data.url, error: e.message });
        }
    } else if (data.type === 'REMOVE_LESSON_OFFLINE') {
        await cache.delete(data.url);
        event.source.postMessage({ type: 'LESSON_REMOVED_SUCCESS', url: data.url });
    } else if (data.type === 'CHECK_OFFLINE_SAVED') {
        const match = await cache.match(data.url);
        event.source.postMessage({ type: 'CHECK_OFFLINE_RESULT', url: data.url, isSaved: !!match });
    }
});

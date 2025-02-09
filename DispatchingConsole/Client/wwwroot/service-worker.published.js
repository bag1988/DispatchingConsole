﻿// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', () => { });

self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  event.waitUntil(clients.matchAll({ includeUncontrolled: true, type: 'window' })
    .then((clientList) => {
    const hadWindowToFocus = clientList.some((windowClient) => {
      return (windowClient.focus(), true) ?? false;
    });
    if (!hadWindowToFocus) clients.openWindow(`${self.location.origin}`).then((windowClient) => windowClient ? windowClient.focus() : null);
  }));
});
self.addEventListener('message', async (event) => {
  if (event.data === 'SKIP_WAITING') {
    self.skipWaiting();
    console.debug('Reload for update service worker...');
    let clientList = await clients.matchAll({ includeUncontrolled: true, type: 'window' });
    if (clientList) {
      try {
        for (let windowClient of clientList) {
          await windowClient.navigate(new URL(windowClient.url).pathname);
        }
      }
      catch (e) {
        console.error(e.message);
        self.registration.unregister();
      }
    }
  }
  else if (event.data.push) {
    try {
      if (self.Notification.permission == "granted") {
        clients.matchAll({ includeUncontrolled: true, type: 'window' })
          .then((clientList) => {
            const hadWindowToFocus = clientList.some((windowClient) => {
              return windowClient.focused;
            });
            if (!hadWindowToFocus) {
              self.registration.showNotification(event.data.payload.Title, {
                body: event.data.payload.Message,
                icon: 'favicon.png',
                vibrate: [100, 50, 100]
              });
            }
          });
      }
    }
    catch (ex) {
      console.error(ex);
    }
  }
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

async function onInstall(event) {
  console.info('Service worker: Install');

  // Fetch and cache all matching items from the assets manifest
  const assetsRequests = self.assetsManifest.assets
    .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
    .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
    .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
  await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
  console.info('Service worker: Activate');

  // Delete unused caches
  const cacheKeys = await caches.keys();
  await Promise.all(cacheKeys
    .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
    .map(key => caches.delete(key)));
}

async function onFetch(event) {
  let cachedResponse = null;
  if (event.request.method === 'GET') {
    // For all navigation requests, try to serve index.html from cache
    // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
    const shouldServeIndexHtml = event.request.mode === 'navigate';

    const request = shouldServeIndexHtml ? 'index.html' : event.request;
    const cache = await caches.open(cacheName);
    cachedResponse = await cache.match(request);
  }
  return cachedResponse || fetch(event.request).catch(e => console.log(e));
}

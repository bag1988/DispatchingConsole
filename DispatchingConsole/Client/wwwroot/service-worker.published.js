// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations
var hub;
self.importScripts("/signalr.min.js");
self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('push', event => {
    const payload = event.data.json();
    event.waitUntil(
        clients.matchAll({
            type: "window"
        }).then((clientList) => {
            const hadWindowToFocus = clientList.some((windowClient) => {
                return windowClient.focused;
            });
            if (!hadWindowToFocus) self.registration.showNotification('Пульт оперативно-диспетчерской связи', {
                body: payload.Message,
                icon: 'favicon.png',
                vibrate: [100, 50, 100],
                requireInteraction: true,
                data: { url: self.location.origin, key: payload.KeyChatRoom }
            });
        })
    );
});
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    // This looks to see if the current is already open and
    // focuses if it is
    event.waitUntil(clients.matchAll({
        type: "window"
    }).then((clientList) => {
        const hadWindowToFocus = clientList.some((windowClient) => {
            return (windowClient.focus(), true) ?? false;
        });
        if (!hadWindowToFocus) clients.openWindow(`${event.notification.data.url}/${event.notification.data.key}`).then((windowClient) => windowClient ? windowClient.focus() : null);
    }));
});
self.addEventListener('message', (event) => {
    if (event.data === 'SKIP_WAITING') {
        self.skipWaiting().then(() => {
            clients.matchAll({
                type: "all"
            }).then((clientList) => {
                clientList.forEach((windowClient) => {
                    windowClient.navigate("/");
                });
            })
        });
    }
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

function connectHub() {
    hub = new signalR.HubConnectionBuilder()
        .withUrl(`${self.location.origin}/CommunicationChatHub`, {
            headers: { "Content-Encoding": "ru" }
        }).withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
                if (retryContext.previousRetryCount < 1) {
                    return 1000;
                } else if (retryContext.previousRetryCount < 2) {
                    return 2000;
                }
                else if (retryContext.previousRetryCount < 3) {
                    return 10000;
                }
                else {
                    return 30000;
                }
            }
        })
        .build();
    hub.on("Fire_ShowPushNotify", function (data) {
        try {
            if (self.Notification.permission == "granted") {
                clients.matchAll({
                    type: "window"
                }).then((clientList) => {
                    const hadWindowToFocus = clientList.some((windowClient) => {
                        return windowClient.focused;
                    });
                    if (!hadWindowToFocus) {
                        const payload = JSON.parse(data);
                        self.registration.showNotification('Пульт оперативно-диспетчерской связи', {
                            body: payload.Message,
                            icon: 'favicon.png',
                            vibrate: [100, 50, 100],
                            requireInteraction: true,
                            data: { key: payload.KeyChatRoom }
                        });
                    }
                });

            }
        }
        catch (ex) {
            console.error(ex);
        }

    });
}

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

    try {
        if (!hub) {
            connectHub();
        }
        if (hub?.state == signalR.HubConnectionState.Disconnected) {
            hub.start();
        }
    }
    catch (ex) {
        console.error(ex);
    }

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

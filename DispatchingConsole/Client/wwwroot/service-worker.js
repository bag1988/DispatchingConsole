// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
var hub;
self.importScripts("/signalr.min.js");
self.addEventListener('install', async event => {
    console.log('Installing service worker...');
    self.skipWaiting();
});
self.addEventListener('fetch', () => {
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
});


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
                data: { key: payload.KeyChatRoom }
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
        if (clientList.length > 0) {
            clientList[0].focus();
        }
        else {
            clients.openWindow(`${self.location.origin}/${event.notification.data.key}`).then((windowClient) => windowClient ? windowClient.focus() : null);
        }
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
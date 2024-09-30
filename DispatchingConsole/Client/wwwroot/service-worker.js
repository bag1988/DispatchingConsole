// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).

self.addEventListener('install', event => {
  console.log('Installing service worker...'); self.skipWaiting();
});
self.addEventListener('fetch', () => { });
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
self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  event.waitUntil(clients.matchAll({ includeUncontrolled: true, type: 'window' })
    .then((clientList) => {
      if (clientList.length > 0) {
        clientList[0].focus();
      }
      else {
        clients.openWindow(`${self.location.origin}`).then((windowClient) => windowClient ? windowClient.focus() : null);
      }
    }));
});

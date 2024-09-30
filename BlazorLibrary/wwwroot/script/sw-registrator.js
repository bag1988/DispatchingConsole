if (('serviceWorker' in navigator)) {
  navigator.serviceWorker.register('/service-worker.js', { updateViaCache: 'none' }).then((registration) => {
    console.debug(`Service worker registration successful (scope: ${registration.scope})`);
    if (registration.onupdatefound == null) {
      console.debug("Подписываемся на прослушку наличия обновления");
      registration.onupdatefound = () => {
        const installingServiceWorker = registration.installing;
        installingServiceWorker.onstatechange = () => {
          console.debug("Зафиксировано изменение состояния sw", installingServiceWorker.state);
          if (installingServiceWorker.state === 'installed' && registration.waiting) {
            console.debug("Доступно обновление");
          }         
        }
      };
    }
  });
}
else {
  console.error(`This browser doesn't support service workers`);
}

window.sendKeepAliveWorker = async () => {
  try {
    if (navigator.serviceWorker && navigator.serviceWorker.controller) {
      var _ready = await navigator.serviceWorker.ready;
      if (_ready && _ready.active) {
        await _ready.active.postMessage("KEEP_ALIVE");
      }
    }
  }
  catch (e) {
    console.error(e.message);
  }
}

window.sendSkipWaitingServiceWorker = async () => {
  try {
    if (navigator.serviceWorker) {
      var _ready = await navigator.serviceWorker.ready;
      console.debug("Отправка запроса на пропуск ожидания, есть ожидающий service worker", _ready?.waiting != null);
      if (_ready && _ready.waiting) {
        await _ready.waiting.postMessage("SKIP_WAITING");
        setTimeout(() => location.reload(), 1000);
        return true;
      }
    }
  }
  catch (e) {
    console.error(e.message);
  }
  console.error("Ошибка отправки запроса на пропуск ожидания");
  return false;
}
window.unregisterServiceWorker = async () => {
  try {
    if (navigator.serviceWorker) {
      var _ready = await navigator.serviceWorker.ready;
      console.debug("Отмена регистрации service worker");
      if (_ready) {
        await _ready.unregister();
        location.reload();
        return true;
      }
    }
  }
  catch (e) {
    console.error(e.message);
  }
  return false;
}

window.checkUpdateServiceWorker = async () => {
  try {
    if (navigator.serviceWorker) {
      var _ready = await navigator.serviceWorker.ready;
      if (_ready) {
        await _ready.update();
        console.debug("Есть активный service worker", _ready.active != null);
        console.debug("Устанавливается service worker", _ready.installing != null);
        console.debug("Состояние контроллера", navigator.serviceWorker.controller?.state);
        if (_ready.installing == null && _ready.active != null && _ready.waiting) {
          console.warn("Результат проверки обновления: есть ожидающий service-worker");
          return true;
        }
      }
    }
    else {
      console.warn("Подключение небезопасно, отсутствует service-worker");
      return false;
    }
  }
  catch (e) {
    console.error(e.message);
  }
  console.debug("Результат проверки обновления: нет ожидающего service-worker");
  return false;
}

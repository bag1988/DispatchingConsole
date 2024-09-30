export const CreateObserver = (dotNet, callBackNet) =>
  new Promise(resolve => {
    const callback = async (entries, observer) => {
      try {
        if (entries && entries.length > 0) {
          await dotNet.invokeMethodAsync(callBackNet, entries[0].target.getBoundingClientRect());
        }
      }
      catch (e) {
        console.error(e);
      }
    };
    const observer = new IntersectionObserver(callback, {
      // родитель целевого элемента - область просмотра
      root: null,
      // без отступов
      rootMargin: '0px',
      // процент пересечения - половина изображения
      threshold: 0.5
    });

    const start = (querySelector) => {
      try {
        if (querySelector) {
          let node = document.querySelector(querySelector);
          if (node) {
            observer.observe(node);
          }
        }
      }
      catch (e) {
        console.error("Error start ResizeObserver for ", querySelector, e);
      }
    };

    const stop = (querySelector) => {
      try {
        if (querySelector) {
          let node = document.querySelector(querySelector);
          if (node) {
            observer.unobserve(node);
          }
        }
        observer.disconnect();
      }
      catch (e) {
        console.error("Error stop ResizeObserver for ", querySelector, e);
      }
    }
    resolve({ start, stop });
  });  
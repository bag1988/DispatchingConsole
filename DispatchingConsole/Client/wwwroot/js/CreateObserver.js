export class CreateObserver {
  static init(dotNet, callBack) {
    this.dotNet = dotNet;
    this.callBack = callBack;
    this.args = null;
    this.currentObj = null;
    this.options = {
      // родитель целевого элемента - область просмотра
      root: null,
      // без отступов
      rootMargin: '0px',
      // процент пересечения - половина изображения
      threshold: 0.5
    };
    this.observer = new IntersectionObserver((entries, observer) => {
      // для каждой записи-целевого элемента
      entries.forEach(async (entry) => {
        // если элемент является наблюдаемым
        if (entry.isIntersecting) {
          try {
            // прекращаем наблюдение
            observer.unobserve(entry.target);
            let top = entry.target.parentElement?.scrollTop;
            let startHeight = entry.target.parentElement?.scrollHeight;
            await this.dotNet.invokeMethodAsync(this.callBack, this.args);
            let endHeight = entry.target.parentElement?.scrollHeight;
            entry.target.parentElement?.scrollTo(0, (top + (endHeight - startHeight)));
          }
          catch (e) {
            console.error("callBack", e.message);
          }
        }
      })
    }, this.options);
  }
  static startObserver(querySelector, argsValue) {
    try {
      this.stopObserver();
      if (querySelector) {
        let node = document.querySelector(querySelector);
        if (node) {
          this.currentObj = node;
          this.args = argsValue;
          this.observer.observe(node);
        }
      }
    }
    catch (e) {
      console.error("startObserver", e.message);
    }
  }
  static stopObserver() {
    try {
      if (this.currentObj) {
        this.observer.unobserve(this.currentObj);
      }
      this.currentObj = null;
      this.args = null;
    }
    catch (e) {
      console.error("stopObserver", e.message);
    }
  }
}
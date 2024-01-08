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
      entries.forEach(entry => {
        // если элемент является наблюдаемым
        if (entry.isIntersecting) {
          try {
            // прекращаем наблюдение
            observer.unobserve(entry.target);
            this.dotNet.invokeMethodAsync(this.callBack, this.args);
          }
          catch (e) {
            console.log(e);
          }
        }
      })
    }, this.options);
  }
  static startObserver(obj, argsValue) {
    this.stopObserver();
    this.currentObj = obj;
    this.args = argsValue;
    this.observer.observe(obj);
  }

  static stopObserver() {
    if (this.currentObj) {
      this.observer.unobserve(this.currentObj);
    }
    this.currentObj = null;
    this.args = null;
  }
}
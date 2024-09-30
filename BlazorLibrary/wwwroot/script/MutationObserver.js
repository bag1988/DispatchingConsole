
export const CreateMutationObserver = (dotNet, callBackNet, config) =>
  new Promise(resolve => {
    if (!config) {
      config = { attributes: false, childList: true, subtree: true };
    }
    const callback = async (mutationList, observer) => {
      if (mutationList.some(e => e.addedNodes || e.removedNodes)) {
        await dotNet.invokeMethodAsync(callBackNet);
      }
    };
    const observer = new MutationObserver(callback);

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
        console.error("Error start MutationObserver for ", querySelector, e.message);
      }
    };
    const stop = () => {
      try {
        observer.disconnect();
      }
      catch (e) {
        console.error("Error stop MutationObserver ", e.message);
      }
    }
    resolve({ start, stop });
  });  
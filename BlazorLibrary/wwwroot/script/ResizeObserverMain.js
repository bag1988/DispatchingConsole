
export const CreateResizeForMainObserver = (dotNet, callBackNet) =>
  new Promise(resolve => {
    const callback = async (entries, observer) => {
      try {
        if (entries && entries.length > 0 && forNode) {         
          await dotNet.invokeMethodAsync(callBackNet, forNode.getBoundingClientRect(), forNode.scrollHeight);
        }
      }
      catch (e) {
        console.error(e);
      }
    };
    const observer = new ResizeObserver(callback);
    let forNode;
    const start = (targetNode) => {
      try {
        if (targetNode) {
          let main = document.querySelector("main");
          if (main) {
            forNode = targetNode;
            observer.observe(main);
          }
        }
      }
      catch (e) {
        console.error("Error start ResizeObserver for ", targetNode, e.message);
      }
    };

    const resize = async () => {
      try {
        if (forNode) {
          await dotNet.invokeMethodAsync(callBackNet, forNode.getBoundingClientRect(), forNode.scrollHeight);
        }
      }
      catch (e) {
        console.error("Error resize for Node", e.message);
      }
    };

    const stop = () => {
      try {
        let main = document.querySelector("main");
        if (main) {
          observer.unobserve(main);
        }
        observer.disconnect();
      }
      catch (e) {
        console.error("Error stop ResizeObserver for main", e.message);
      }
    }
    resolve({ start, stop, resize });
  });  
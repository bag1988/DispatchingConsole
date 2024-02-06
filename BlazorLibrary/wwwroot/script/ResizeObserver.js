
export const CreateResizeObserver = (dotNet, callBackNet) =>
  new Promise(resolve => {
    const callback = async (entries, observer) => {      
      try {
        if (entries && entries.length > 0) {
          await dotNet.invokeMethodAsync(callBackNet, entries[0].target.getBoundingClientRect());
        }
      }
      catch (e) {
        console.log(e);
      }
    };
    const observer = new ResizeObserver(callback);

    const start = (targetNode) => {
      try {
        if (targetNode) {
          observer.observe(targetNode);
        }
      }
      catch (e) {
        console.log("Error start ResizeObserver for ", targetNode, e);
      }     
    };

    const stop = (targetNode) => {

      try {
        if (targetNode) {
          observer.unobserve(targetNode);
        }
        observer.disconnect();
      }
      catch (e) {
        console.log("Error stop ResizeObserver for ", targetNode, e);
      }
    }
    resolve({ start, stop });
  });  

export const CreateMutationObserver = (dotNet, callBackNet) =>
  new Promise(resolve => {
    const config = { attributes: false, childList: true, subtree: true };
    const callback = async (mutationList, observer) => {
      if (mutationList.some(e => e.addedNodes || e.removedNodes)) {
        await dotNet.invokeMethodAsync(callBackNet);
      }      
    };
    const observer = new MutationObserver(callback);

    const start = (targetNode) => {
      if (targetNode) {
        observer.observe(targetNode, config);
      }
      else {
        console.log("Error start MutationObserver for ", targetNode);
      }
    };

    const stop = (targetNode) => {
      console.log("Stop MutationObserver for ", targetNode);
      observer.disconnect();
    }
    resolve({ start, stop });
  });  
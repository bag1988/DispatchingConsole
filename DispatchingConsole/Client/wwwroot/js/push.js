
export async function SendPush(jsonData) {
  try {
    if (navigator.serviceWorker) {
      var readySw = await navigator.serviceWorker.ready;
      if (readySw && readySw.active) {
        const payload = JSON.parse(jsonData);
        readySw.active.postMessage({ push: true, payload: payload });
      }
    }
  }
  catch (e) {
    console.log(e);
  }
}
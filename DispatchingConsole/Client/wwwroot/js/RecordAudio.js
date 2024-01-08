export const recordAudio = () =>
  new Promise(async resolve => {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    const mediaRecorder = new MediaRecorder(stream);
    let audioChunks = [];

    mediaRecorder.addEventListener("dataavailable", event => {
      audioChunks.push(event.data);
    });

    const start = () => {
      mediaRecorder.start();
    };

    const stop = () =>
      new Promise(resolve => {
        mediaRecorder.addEventListener("stop", () => {
          const audioBlob = new Blob(audioChunks, { type: mediaRecorder.mimeType });
          stream.getTracks().forEach((track) => track.stop());
          audioChunks = [];
          resolve(audioBlob);
        });
        mediaRecorder.stop();
      });
    resolve({ start, stop });
  });  
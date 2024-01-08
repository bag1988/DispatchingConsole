export class VideoRecord {
  constructor(stream, dotNet, keyChatRoom, fileName) {
    this.stream = stream;
    this.dotNet = dotNet;
    this.keyChatRoom = keyChatRoom;
    this.fileName = fileName
    this.recorder = null;
    this.recorder = new MediaRecorder(stream);
    this.recorder.ondataavailable = (e) => {
      try {
        //console.log("send data", e);
        this.sendToserver(e.data, this.dotNet);
      }
      catch (e) {
        this.stop();
        console.log(e);
      }
    };
  }
  sendToserver(blobData, dotNet) {
    var fileReader = new FileReader();
    fileReader.onload = async function (event) {
      await dotNet.invokeMethodAsync("StreamToFile", new Uint8Array(event.target.result));
    };
    fileReader.readAsArrayBuffer(blobData);
  }
  getBlob() {
    return new Blob(this.chunks, { type: "video/webm" });
  }
  start() {
    //this.chunks = [];
    this.recorder.start(1000);
  }
  stop() {
    this.recorder.stop();
    return new Promise(() => {      
      this.recorder.onstop = async () => {
        await this.dotNet.invokeMethodAsync("StopStreamToFile");       
      };
    });
  }
}
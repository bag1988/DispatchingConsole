export class ScreenAndCamMixer {
  constructor() {
    this.screenShareVideoRef = document.createElement("video");
    this.webCamVideoRef = document.createElement("video");
    this.canvas = document.createElement("canvas");
    this.canvas.width = 1024;
    this.canvas.height = 768;
    this.canvasContext = this.canvas.getContext("2d");
    this.frameInterval = 25;
  }
  async startMixing() {
    this.webCamVideoRef.srcObject = await navigator.mediaDevices.getUserMedia({ video: true });
    this.webCamVideoRef.play();

    this.screenShareVideoRef.srcObject = await navigator.mediaDevices.getDisplayMedia({ video: true });
    this.screenShareVideoRef.play();
    if (this.screenShareVideoRef.srcObject && this.screenShareVideoRef.srcObject.getVideoTracks()[0]) {
      this.canvas.width = this.screenShareVideoRef.srcObject.getVideoTracks()[0].getSettings().width;
      this.canvas.height = this.screenShareVideoRef.srcObject.getVideoTracks()[0].getSettings().height;
      this.frameInterval = this.screenShareVideoRef.srcObject.getVideoTracks()[0].getSettings().frameRate;
    }
    this.drawVideosToCanvas();
  }
  drawVideosToCanvas() {
    if (this.webCamVideoRef.srcObject || this.screenShareVideoRef.srcObject) {
      this.canvasContext.fillRect(0, 0, this.canvas.width, this.canvas.height);
      this.drawImage();
      setTimeout(this.drawVideosToCanvas.bind(this), this.frameInterval);
    }
  }
  drawImage() {
    try {
      this.canvasContext.drawImage(this.screenShareVideoRef, 0, 0, this.canvas.width, this.canvas.height);
      let w = Math.round(this.canvas.width * 0.3);
      let h = Math.round(this.canvas.height * 0.3);
      this.canvasContext.drawImage(this.webCamVideoRef, this.canvas.width - w, this.canvas.height - h, w, h);
    }
    catch (e) {
      console.log(e);
    }
  }
  StopMixed() {
    try {
      this.webCamVideoRef.srcObject?.getVideoTracks().forEach(track => {
        track.stop();
      });
      this.webCamVideoRef.srcObject = null;
      this.screenShareVideoRef.srcObject?.getVideoTracks().forEach(track => {
        track.stop();
      });
      this.screenShareVideoRef.srcObject = null;
    }
    catch (e) {
      console.log(e);
    }
  }
  getMixedVideoStream() {
    return this.canvas.captureStream();
  }
}
export class VideoMixer {
  constructor(videos) {
    this.canvas = document.createElement("canvas");
    this.canvasContext = this.canvas.getContext("2d");
    this.canvas.width = /*videos[0].srcObject?.getVideoTracks()[0]?.getSettings().width ??*/ 1024;
    this.canvas.height = /*videos[0].srcObject?.getVideoTracks()[0]?.getSettings().height ??*/ 768;
    this.videos = [];
    this.gainNode = null;
    this.captureStream = null;
    this.audioSources = [];
    this.audioDestination = null;
    this.imageSound = null;
    this.cell = 1;
    this.itemSize = { width: this.canvas.width, height: this.canvas.height };
    this.videos = videos;
    this.frameInterval = videos[0].srcObject.getVideoTracks()[0]?.getSettings().frameRate ?? 25;
    this.videos.forEach((v, i) => {
      //console.log("forEach videos", v.srcObject.getTracks());
      this.setEventVideo(v);
    });
    let img = new Image();
    img.src = "/sound.svg";
    img.onload = () => {
      this.imageSound = img;
    }
  }
  startMixing() {
    //console.log("startMixing -------------------------------");
    this.isStopDrawingFrames = false;
    this.calcSize();
    this.drawVideosToCanvas();
    this.createMixedStream();
  }
  setEventVideo(video) {
    //console.log("setEventVideo ", video.srcObject.getTracks());

    video.onloadedmetadata = (e) => {
      //console.log("setEventVideo -> onloadedmetadata", e.target);      
      this.calcSize();
    };


  }
  eventForVideoTrack(forKey, videoTrack) {
    if (this.videos) {
      let videoElem = this.videos.find(v => v.getAttribute("for") == forKey);
      //console.log(videoTrack);
      if (videoElem) {
        if (videoTrack) {
          videoTrack.onmute = (event) => {
            try {
              videoElem.srcObject.removeTrack(event.currentTarget);
              videoElem.load();
            }
            catch (e) {
              console.log((new Date()).toLocaleString(), "VideoMixer error onmute", e);
            }
          };
          videoTrack.onunmute = (event) => {
            try {
              videoElem.srcObject.addTrack(event.currentTarget);
            }
            catch (e) {
              console.log((new Date()).toLocaleString(), "VideoMixer error onunmute", e);
            }
          };
        }
      }
    }
  }
  addVideos(video) {
    //console.log("addVideos ", video.srcObject.getTracks());
    this.videos.push(video);
    this.setEventVideo(video);
    this.addMixedAudioStream(video);
  }
  removeVideo(forkey) {
    //console.log("removeVideo forkey", forkey);
    let current = this.videos.find(v => v.getAttribute("for") == forkey);
    if (current) {
      current.srcObject?.getTracks().forEach(track => {
        track.stop();
        current.srcObject.removeTrack(track);
      });
    }
    this.videos = this.videos.filter(v => v.getAttribute("for") != forkey);
    if (this.videos.length == 0) {
      this.isStopDrawingFrames = true;
    }
    this.calcSize();
  }
  removeAllVideo() {
    //console.log("removeAllVideo -------------------------------");
    this.videos?.forEach(v => {
      v.srcObject?.getTracks().forEach(track => {
        track.stop();
        v.srcObject.removeTrack(track);
      });
    });

    this.videos = [];
    this.isStopDrawingFrames = true;
    this.calcSize();
    this.audioContext.close();
    this.audioContext = null;

    this.captureStream?.getTracks().forEach(track => {
      track.stop();
      this.captureStream.removeTrack(track);
    });
    this.captureStream = null;
  }
  calcSize() {
    this.cell = Math.ceil(Math.sqrt(this.videos.length));
    this.row = this.videos.length > 2 ? this.cell : 1;
    this.itemSize.width = Math.floor(this.canvas.width / this.cell);
    this.itemSize.height = Math.floor(this.canvas.height / this.cell);
  }
  drawVideosToCanvas() {
    this.canvasContext.fillRect(0, 0, this.canvas.width, this.canvas.height);

    this.videos.forEach((video, index) => {
      this.drawImage(video, index);
    });
    if (!this.isStopDrawingFrames) {
      setTimeout(this.drawVideosToCanvas.bind(this), this.frameInterval);
    }
  }
  drawImage(video, index) {
    try {
      ++index;
      let startRow = Math.ceil(index / this.cell);
      let startCell = index - (startRow * this.cell) + this.cell;
      --startRow;
      --startCell;
      let x = startCell * this.itemSize.width;
      let y = startRow * this.itemSize.height;

      let settings;

      var t = video.srcObject?.getVideoTracks();
      if (t?.length > 0) {
        settings = t[0].getSettings();
      }
      else {
        settings = this.imageSound ?? {
          width: this.itemSize.width,
          height: this.itemSize.height
        };
      }

      let propCurrentSize = settings.width / settings.height;

      let propNeedSize = (this.itemSize.width / this.itemSize.height);

      let currentWidth = this.itemSize.width;
      let currentHeight = this.itemSize.height;

      if (propNeedSize > propCurrentSize) { //если во входящем видео высота больше ширины
        currentWidth = settings.width * (this.itemSize.height / settings.height);
        x = (x + Math.min((this.itemSize.width - currentWidth) / 2));
      }
      else if (propNeedSize < propCurrentSize) { //если во входящем видео ширина больше высоты
        currentHeight = settings.height * (this.itemSize.width / settings.width);
        y = (y + Math.min((this.itemSize.height - currentHeight) / 2));
      }

      if (this.videos.length <= (this.cell * (this.cell - 1))) {
        y = (y + this.itemSize.height / 2);
      }

      if (t?.length > 0) {
        this.canvasContext.drawImage(video, x, y, currentWidth, currentHeight);
      }
      else if (this.imageSound) {
        this.canvasContext.fillStyle = 'white';
        this.canvasContext.fillRect(startCell * this.itemSize.width, y, this.itemSize.width, this.itemSize.height);
        this.canvasContext.drawImage(this.imageSound, x, y, currentWidth, currentHeight);
      }
      this.canvasContext.fillStyle = 'black';
      this.canvasContext.font = "24px serif";
      this.canvasContext.fillText(video.getAttribute("for"), x, (y + 24));
      this.canvasContext.fillStyle = 'white';
      this.canvasContext.font = "24px serif";
      this.canvasContext.fillText(video.getAttribute("for"), x - 1, (y + 23));
      this.canvasContext.fillStyle = 'black';
    }
    catch (e) {
      console.log(e);
    }
  }
  createMixedStream() {
    const audioStream = this.getMixedAudioStream();

    this.captureStream = this.canvas.captureStream(this.frameInterval);
    if (audioStream !== null) {
      audioStream.getTracks()?.forEach((audioTrack) => {
        this.captureStream.addTrack(audioTrack);
      });
    }
  }

  getCaptureStream() {
    return this.captureStream;
  }

  getMixedAudioStream() {
    try {
      //if (!this.audioContext)
      this.audioContext = (typeof AudioContext !== "undefined") ? new AudioContext() : (typeof window.webkitAudioContext !== "undefined") ? new window.webkitAudioContext() : (typeof window.mozAudioContext !== "undefined") ? new window.mozAudioContext() : "undefined";

      this.gainNode = this.audioContext.createGain();
      this.gainNode.connect(this.audioContext.destination);
      this.gainNode.gain.value = 0; // don't hear self

      //console.log("count videos", this.videos.length);

      this.audioDestination = this.audioContext.createMediaStreamDestination();
      this.videos.forEach((v) => this.addMixedAudioStream(v));
      return this.audioDestination.stream;
    }
    catch (e) {
      console.log(e);
    }
  }
  addMixedAudioStream(video) {
    try {

      //console.log("add mixed audio for ", video);

      if (this.audioContext && this.gainNode && this.audioDestination) {
        let audioSource = this.audioContext.createMediaStreamSource(video.srcObject);
        audioSource.connect(this.gainNode);
        audioSource.connect(this.audioDestination);
      }
    }
    catch (e) {
      console.log(e);
    }
  }
}

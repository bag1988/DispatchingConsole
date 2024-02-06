
function getCultureGlobal() {
  return window.localStorage['CultureGlobal'];
};

function setCultureGlobal(value) {
  window.localStorage['CultureGlobal'] = value;
};
//для FiltrInput
function GetElemLeftPosition(elem) {
  if (elem) {
    var parenElem = elem.parentElement?.parentElement;
    parenElem?.scrollTo(elem.offsetLeft, 0);
    return elem.offsetLeft - parseInt(parenElem?.scrollLeft);
  }
}
//для MultiSelectVirtualize
function ScrollToSelectElement(elem, querySelector) {
  if (elem) {
    if (elem.querySelector(querySelector))
      elem.querySelector(querySelector).scrollIntoView({ block: "center" });
  }
}

function GetBoundingClientRect(div) {
  if (div) {
    return div.getBoundingClientRect();
  }
  return null;
}

function GetWindowHeight(div) {
  var newHeight = 0;
  if (div && div.offsetTop) {
    var topElem = div.offsetTop + 15;
    newHeight = (window.innerHeight - topElem);
  }
  else
    newHeight = window.innerHeight;

  return newHeight;
}

function CloseWindows(ref) {
  window.addEventListener('beforeunload', async (event) => {

    await ref.invokeMethodAsync("CloseWindows");
    // Отмените событие, как указано в стандарте.
    event.preventDefault();
    // Chrome требует установки возвратного значения.
    event.returnValue = '';

  });
}

async function InitAudioPlayer(audio, deviceid, volum) {
  try {
    if (audio && audio.constructor == HTMLAudioElement) {
      if (deviceid) {
        try {
          var dList = await getEnumerateDevices();
          if (dList && dList.filter(x => x.kind === "audiooutput" && x.deviceId == deviceid).length > 0) {
            await audio.setSinkId(deviceid);
          }
        }
        catch (e) {
          console.log(e);
        }
      }
      if (volum && volum > 0) {
        if (volum == 0)
          volum = 1;
        audio.volume = volum / 100;
      }
    }
  }
  catch (e) {
    console.log(e);
  }
}

//проверяем разрешения и получаем список устройств
async function GetAudioTrack() {
  try {
    if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
      alert("Ваш браузер не поддерживает перечисление устройств!");
      return null;
    }

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      alert('Ваш браузер не поддерживает запись!');
      return null;
    }

    let result = await navigator.permissions.query({ name: 'microphone' });
    if (result) {
      if (result.state == "denied" || result.state == "prompt") {
        (await navigator.mediaDevices.getUserMedia({ audio: true })).getTracks().forEach(track => track.stop());
      }
      return await getEnumerateDevices();
    }
  }
  catch (e) {
    console.log(e);
  }
  return null;
}

//получаем список устройств
async function getEnumerateDevices() {
  try {
    if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
      alert('Ваш браузер не поддерживает перечисление устройств!');
      return null;
    }
    let devices = await navigator.mediaDevices.enumerateDevices();
    return devices;
  }
  catch (e) {
    console.log(e);
  }
}

//для ModalDialog
function GetMaxIndexModal() {

  var ModalList = document.querySelectorAll(".offcanvas");

  if (ModalList) {
    var IndexArray = Array.prototype.map.call(ModalList, (x) => parseInt(x.style.zIndex));
    var zIndex = Math.max.apply(null, IndexArray);
    if (zIndex > 0)
      return zIndex;
  }
  return 1041;
}

/*var mediaRecorder;*/
var RecordAudio = {
  MediaStream: null,
  StreamSource: null,
  AudioCtx: null,
  WorkletNode: null,
  RefInvoke: null,
  StartStreamWorklet: async function (param, ref) {

    RecordAudio.StopStream();

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      await ref.invokeMethodAsync("ErrorRecord", null);
      return null;
    }
    //params = param;
    return new Promise(async (resolve) => {
      var deviceId = "default";
      if (param && param.label) {
        var dList = await getEnumerateDevices();
        if (dList && dList.filter(x => x.deviceId == param.label).length > 0) {
          deviceId = dList.find(x => x.deviceId == param.label).deviceId;
        }
      }

      var constraints = {
        audio: {
          deviceId: { exact: deviceId },
          sampleRate: param.sampleRate,
          sampleSize: param.sampleSize,
          channelCount: param.channelCount
        },
        video: false
      };

      navigator.mediaDevices.getUserMedia(constraints)
        .then(async function (mediaStream) {

          RecordAudio.MediaStream = mediaStream;

          RecordAudio.AudioCtx = new AudioContext({ sampleRate: param.sampleRate });

          RecordAudio.StreamSource = RecordAudio.AudioCtx.createMediaStreamSource(RecordAudio.MediaStream);

          if (param.volum) {

            if (param.volum == 0)
              param.volum = 1;

            var dest = RecordAudio.AudioCtx.createMediaStreamDestination();
            var gainNode = RecordAudio.AudioCtx.createGain();

            RecordAudio.StreamSource.connect(gainNode);
            gainNode.connect(dest);

            gainNode.gain.value = param.volum / 100;
          }


          var response = { "ChannelCount": param.channelCount, "SampleRate": param.sampleRate, "SampleSize": param.sampleSize };
          // NEW A: Loading the worklet processor                    
          await RecordAudio.AudioCtx.audioWorklet.addModule("/_content/SensorM.GsoUi.BlazorLibrary/script/recorder.worklet.js");

          // Create the recorder worklet
          RecordAudio.WorkletNode = new AudioWorkletNode(RecordAudio.AudioCtx, "recorder.worklet", {
            processorOptions: response
          });

          RecordAudio.RefInvoke = ref;
          RecordAudio.WorkletNode.port.addEventListener("message", RecordAudio.OnMessage, false);
          RecordAudio.WorkletNode.port.start();

          RecordAudio.StreamSource.connect(RecordAudio.WorkletNode);

          let isRecording = RecordAudio.WorkletNode.parameters.get('isRecording');
          isRecording.setValueAtTime(1, RecordAudio.AudioCtx.currentTime);

          resolve(response);
        })
        .catch(async function (err) {
          console.log(err.name + ": " + err.message);
          RecordAudio.StopStream();
          await ref.invokeMethodAsync("ErrorRecord", err.message);
          resolve(null);
        });
    });
  },

  StopStream: function () {
    try {
      if (RecordAudio.WorkletNode && RecordAudio.AudioCtx) {
        let isRecording = RecordAudio.WorkletNode.parameters.get('isRecording');
        isRecording.setValueAtTime(0, RecordAudio.AudioCtx.currentTime);
        return true;
      }
    }
    catch (e) {
      console.log(e);
    }
    return false;
  },

  Float32BufferToBytes: function (abuffer) {
    var r = [];

    if (abuffer.length > 0) {
      var numOfChan = abuffer.length;
      var length = abuffer[0].length * numOfChan * 2;
      var buffer = new ArrayBuffer(length);
      var view = new DataView(buffer);
      var pos = 0, offset = 0;

      while (pos < length) {
        for (i = 0; i < numOfChan; i++) {            // interleave channels
          sample = Math.max(-1, Math.min(1, abuffer[i][offset])); // clamp
          sample = (0.5 + sample < 0 ? sample * 32768 : sample * 32767) | 0; // scale to 16-bit signed int
          view.setInt16(pos, sample, true);          // write 16-bit sample  
          pos += 2;
        }
        offset++                                     // next source sample
      }
      r = Array.prototype.slice.call(new Uint8Array(buffer));

    }
    return r;
  },
  OnMessage: async function (event) {
    if (event.data.eventType == "data" && event.data.audioBuffer.length > 0) {
      var dataArray = RecordAudio.Float32BufferToBytes(event.data.audioBuffer);
      await RecordAudio.RefInvoke.invokeMethodAsync("StreamToAudio", btoa(dataArray.reduce((data, byte) => data + String.fromCharCode(byte), '')));
    }
    if (event.data.eventType == "stop") {
      if (RecordAudio.MediaStream) {
        RecordAudio.MediaStream.getAudioTracks().forEach(track => {
          track.stop();
        });
      }
      RecordAudio.MediaStream = null;
      await RecordAudio.RefInvoke.invokeMethodAsync("StopWorklet");
      RecordAudio.RefInvoke = null;
      if (RecordAudio.AudioCtx)
        RecordAudio.AudioCtx.close();
      RecordAudio.AudioCtx = null;
      RecordAudio.WorkletNode.port.removeEventListener("message", RecordAudio.OnMessage);
      RecordAudio.WorkletNode.port.close();
      RecordAudio.WorkletNode = null;
      RecordAudio.StreamSource.disconnect();
      RecordAudio.StreamSource = null;
      return;
    }
  }
}

function playSoundForUrl(url, isLoop) {
  try {
    var audio = new Audio(url);
    if (isLoop)
      audio.loop = isLoop;
    audio.play();
    return audio;
  }
  catch (e) {
    console.log(e);
  }
  return null;
}

function RemoveBlob(blob) {
  try {
    URL.revokeObjectURL(blob);
  }
  catch {
    console.log("Error RemoveBlob");
  }
}


function CreateObjUrl(elementInput) {
  if (elementInput) {
    try {
      return URL.createObjectURL(elementInput.files[0]);
    }
    catch
    {
      console.log("Ошибка получения файла");
    }
  }
}

async function downloadFileFromStream(fileName, contentStreamReference) {

  const arrayBuffer = await contentStreamReference.arrayBuffer();

  const blob = new Blob([arrayBuffer]);

  const url = URL.createObjectURL(blob);

  triggerFileDownload(fileName, url);

  URL.revokeObjectURL(url);
}

function triggerFileDownload(fileName, url) {
  const anchorElement = document.createElement('a');
  anchorElement.href = url;
  if (fileName) {
    anchorElement.download = fileName;
  }

  anchorElement.click();
  anchorElement.remove();
}

var HotKeys = {
  KeyCurrent: "",
  CodeArray: { ArrowUp: 38, ArrowDown: 40, ArrowRight: 39, ArrowLeft: 37, Control: 17, Alt: 18, Tab: 9, Shift: 16, Escape: 27, Enter: 13, Delete: 46, Insert: 45 },
  ListenWindowKey: function () {
    window.onkeydown = function (event) {
      if (event.ctrlKey || event.shiftKey || event.keyCode == HotKeys.CodeArray.Alt) {
        return;
      }


      if (!event.altKey && event.keyCode != HotKeys.CodeArray.Enter && event.keyCode != HotKeys.CodeArray.Delete && event.keyCode != HotKeys.CodeArray.Insert && event.keyCode != HotKeys.CodeArray.Escape) {
        if (document.activeElement.constructor == HTMLBodyElement && (event.keyCode == HotKeys.CodeArray.ArrowUp || event.keyCode == HotKeys.CodeArray.ArrowDown)) {
          HotKeys.SetFirstElemFocus();
        }
        return;
      }

      if (event.altKey) {
        HotKeys.KeyCurrent = `${HotKeys.CodeArray.Alt}&${event.keyCode}`;
      }
      else {
        HotKeys.KeyCurrent = event.keyCode;
      }


      if (document.activeElement.constructor == HTMLBodyElement || document.activeElement.constructor == HTMLButtonElement) {
        HotKeys.SetFirstElemFocus();
      }
      var child = document.activeElement;
      let maxModal = HotKeys.GetMaxModal();
      if (maxModal && !maxModal.contains(child)) {
        maxModal.querySelector(".offcanvas-body").focus();
      }
      if ((HotKeys.KeyCurrent && !event.altKey && (event.keyCode == HotKeys.CodeArray.Enter || event.keyCode == HotKeys.CodeArray.Escape)) || (event.target.constructor != HTMLInputElement && event.target.constructor != HTMLTextAreaElement && event.target.constructor != HTMLSelectElement)) {

        if (child && event.keyCode == HotKeys.CodeArray.Enter && (child.constructor == HTMLButtonElement || child.constructor == HTMLAnchorElement || child.classList.contains("bg-select") || child.classList.contains("bg-focus"))) {
          return true;
        }
        else {
          //если есть модульное окно и это окно содержит элемент с нужным сочетанием клавиш
          if (maxModal && maxModal.querySelector("[hotkey=\"" + (HotKeys.KeyCurrent) + "\"]")) {
            [...maxModal.querySelectorAll("[hotkey=\"" + (HotKeys.KeyCurrent) + "\"]")].at(-1).click();
          }//если есть модульное окон и есть форма и нажата Enter
          else if (maxModal && maxModal.querySelector("form") && event.keyCode == HotKeys.CodeArray.Enter && (!child.classList.contains("bg-select") && !child.classList.contains("bg-focus"))) {
            //let sumbit = maxModal.querySelector("[type=submit]");
            //if (sumbit) {
            //  sumbit.click();
            //}
          }//если нет модульных окон и есть элемент с нужным сочетанием клавиш
          else if (document.querySelector("[hotkey=\"" + (HotKeys.KeyCurrent) + "\"]") && !document.querySelector(".offcanvas")) {
            var parentElem = event.target;
            if (parentElem.constructor == HTMLBodyElement) {
              parentElem = document.querySelector("main");
            }
            while (parentElem.constructor != HTMLBodyElement) {
              if (parentElem.querySelector("[hotkey=\"" + (HotKeys.KeyCurrent) + "\"]")) {
                [...parentElem.querySelectorAll("[hotkey=\"" + (HotKeys.KeyCurrent) + "\"]")].at(-1).click();
                break;
              }
              //else if (event.keyCode == HotKeys.CodeArray.Enter && parentElem.querySelector("form") && (!child.classList.contains("bg-select") && !child.classList.contains("bg-focus"))) {
              //  let sumbit = parentElem.querySelector("[type=submit]");
              //  if (sumbit) {
              //    sumbit.click();
              //  }
              //  break;
              //}
              else
                parentElem = parentElem.parentElement;
            }
          }
        }
      }
      return true;
    }
  },
  SetFirstElemFocus: function () {
    var elem = HotKeys.GetMaxModal();
    if (!elem) {
      elem = document.querySelector("main");
    }
    if (!elem.contains(document.activeElement)) {
      if (elem.querySelector("[tabindex]"))
        elem.querySelector("[tabindex]").focus();
      else if (elem.querySelector("input"))
        elem.querySelector("input").focus();
      else if (elem.querySelector("button"))
        elem.querySelector("button").focus();
    }
  },
  GetMaxModal: function () {
    var elem;
    var index = 0;
    document.querySelectorAll(".offcanvas")?.forEach((x, i) => {
      var zIndex = x.style.zIndex;
      if (zIndex > index) {
        elem = x;
        index = zIndex;
      }
    });
    if (elem) {
      return elem;
    }
    return null;
  },
  SetFocusLink: function (elem, index) {
    if (elem) {

      if (document.activeElement && (document.activeElement.constructor == HTMLInputElement || document.activeElement.constructor == HTMLTextAreaElement || document.activeElement.constructor == HTMLSelectElement)) {
        return;
      }
      var linkArray = elem.querySelector("nav")?.querySelectorAll("a, .nav-link");
      var newFocus;
      if (document.activeElement && document.activeElement.constructor == HTMLAnchorElement && elem.contains(document.activeElement)) {
        linkArray.forEach((x, i) => {
          if (x == document.activeElement) {
            if (index == -1) {
              if ((i - 1) >= 0)
                newFocus = linkArray[(i - 1)];
              else
                newFocus = [...linkArray].at(-1);
            }
            else {
              if ((i + 1) < linkArray.length) {
                newFocus = linkArray[(i + 1)];
              }
              else
                newFocus = [...linkArray].at(0);
            }
            return;
          }
        });
      }
      else {
        newFocus = [...linkArray].at(0);
      }
      if (newFocus)
        newFocus.focus();
    }
  }
}
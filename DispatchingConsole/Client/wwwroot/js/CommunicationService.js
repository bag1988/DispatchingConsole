import { ScreenAndCamMixer } from './ScreenAndCamMixer.js';
import { VideoMixer } from './VideoMixer.js';
import { VideoRecord } from './RecordToServer.js';

var mediaStreamConstraints;

// Set up to exchange only video.
const offerOptions = {
  offerToReceiveAudio: 1,
  offerToReceiveVideo: 1
};

const servers = null;

const peerConnectionArray = new Map();

let dotNet;
let localStream;
let screenAndCamMixer;
let videoMixer;
let localIdPlayer;
let remoteIdPlayerArray;
let videoRecord;
let cuurentType = "";

export function initialize(dotNetRef, _localIdPlayer, _remoteIdPlayerArray) {
  dotNet = dotNetRef;
  localIdPlayer = _localIdPlayer;
  remoteIdPlayerArray = _remoteIdPlayerArray;
  screenAndCamMixer = new ScreenAndCamMixer();
}

function getRemoteArrayPlayer() {
  if (document.querySelector("#" + remoteIdPlayerArray))
    return document.querySelector("#" + remoteIdPlayerArray);
  return null;
}

function getLocalPlayer() {
  if (document.querySelector("#" + localIdPlayer))
    return document.querySelector("#" + localIdPlayer);
  return null;
}


function getMedia(constraints) {
  return new Promise((resolve) => {
    navigator.mediaDevices.getUserMedia(constraints)
      .then((mediaStream) => {
        resolve(mediaStream);
      })
      .catch(function (err) {
        console.log((new Date()).toLocaleString(), err.name + ": " + err.message);
        resolve(null);
      });
  });
}

export async function ChangeLocalStream(typeChange) {
  try {
    if (localStream) {
      screenAndCamMixer.StopMixed();
      localStream.getVideoTracks().forEach(track => {
        track.stop();
        localStream.removeTrack(track);
      });
      let mediaStream;
      if (typeChange == "ScreenOn") {
        if (cuurentType == "VideoOn") {
          await screenAndCamMixer.startMixing();
          mediaStream = screenAndCamMixer.getMixedVideoStream();
        }
        else {
          mediaStream = await navigator.mediaDevices.getDisplayMedia({
            video: {
              displaySurface: "window"
            }
          });
        }
      }
      else if (typeChange == "VideoOn") {
        mediaStream = await getMedia({ video: true });
      }

      cuurentType = typeChange;

      if (mediaStream) {
        mediaStream.getVideoTracks().forEach(track => {
          if (track)
            localStream.addTrack(track);
        });
      }

      var localPlayer = getLocalPlayer();
      if (localPlayer) {
        localPlayer.srcObject = null;
        localPlayer.srcObject = localStream;
      }
      return true;
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error change local stream", e);
  }
  return false;
}
export function ChangeSoundTrack(isSoundRecord) {
  if (localStream) {
    localStream.getAudioTracks()[0].enabled = isSoundRecord;
  }
}

function GetNameKey(forUrl, forUser) {
  return `${forUrl}-${forUser}`;
}
export async function StartOfferForChangeStream(forUrl, forUser, keyChatRoom) {
  try {
    if (peerConnectionArray.has(GetNameKey(forUrl, forUser))) {
      localStream.getTracks().forEach(l => {
        var sender = peerConnectionArray.get(GetNameKey(forUrl, forUser))
          .getSenders().find((s) => s?.track?.kind === l.kind);
        if (sender) {
          sender.replaceTrack(l);
        }
        else {
          peerConnectionArray.get(GetNameKey(forUrl, forUser)).addTrack(l, localStream);
        }
      });

      let offerDescription = await peerConnectionArray.get(GetNameKey(forUrl, forUser)).createOffer(offerOptions);
      await peerConnectionArray.get(GetNameKey(forUrl, forUser)).setLocalDescription(offerDescription);
      return JSON.stringify(offerDescription);
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error add video to local stream", e);
  }
  return null;
}

export async function StartAnswerForChangeStream(forUrl, forUser, keyChatRoom, descriptionText) {
  try {
    if (peerConnectionArray.has(GetNameKey(forUrl, forUser))) {
      let description = JSON.parse(descriptionText);

      await peerConnectionArray.get(GetNameKey(forUrl, forUser)).setRemoteDescription(description);
      let answer = await peerConnectionArray.get(GetNameKey(forUrl, forUser)).createAnswer();
      await peerConnectionArray.get(GetNameKey(forUrl, forUser)).setLocalDescription(answer);
      return JSON.stringify(answer);
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error add video to local stream", e);
  }
  return null;
}

async function startLocalStream(keyChatRoom) {
  try {

    cuurentType = mediaStreamConstraints.video ? "VideoOn" : "";

    if (!localStream)
      localStream = await getMedia(mediaStreamConstraints);


    var localPlayer = getLocalPlayer();

    if (!localPlayer.srcObject && localStream) {

      //console.log("startLocalStream ------------------------------");

      localPlayer.srcObject = localStream;
      localPlayer.classList.remove("d-none");

      videoMixer = new VideoMixer([localPlayer]);
      videoMixer.startMixing();
      startRecord(keyChatRoom);
    }
  }
  catch (e) {
    console.log("Error start local stream", e);
  }
}

async function startRecord(keyChatRoom) {
  try {
    let fileName = await dotNet.invokeMethodAsync("StartRecord", keyChatRoom);
    if (fileName) {
      if (videoRecord) {
        videoRecord.stop();
        videoRecord = null;
      }
      videoRecord = new VideoRecord(videoMixer.getCaptureStream(), dotNet, keyChatRoom, fileName);
      videoRecord.start();
    }
  }
  catch (e) {
    console.log(e);
  }
}

async function createPeerConnection(forUrl, forUser, keyChatRoom) {

  try {
    //console.log((new Date()).toLocaleString(), "Create P2P ", forUrl);

    if (peerConnectionArray.has(GetNameKey(forUrl, forUser))) return;

    // Create peer connections and add behavior.
    var newPeerConnection = "hello";
    newPeerConnection = new RTCPeerConnection(servers);

    //Created local peer connection object peerConnection.
    newPeerConnection.addEventListener("icecandidate", (event) => { handleConnection(event, forUrl, forUser, keyChatRoom) });
    newPeerConnection.addEventListener("iceconnectionstatechange", (event) => { handleConnectionChange(event, forUrl, forUser, keyChatRoom) });
    newPeerConnection.addEventListener("addstream", (event) => { gotRemoteMediaStream(event, forUrl, forUser, keyChatRoom) });

    await startLocalStream(keyChatRoom);
    // Add local stream to connection and create offer to connect.
    if (localStream) {
      localStream.getTracks().forEach((track) => {
        newPeerConnection.addTrack(track, localStream);
      });
      //console.log((new Date()).toLocaleString(), "Add P2P for ", forUrl);
      peerConnectionArray.set(GetNameKey(forUrl, forUser), newPeerConnection);
    }
    else
      newPeerConnection.close();
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error create P2P", e);
  }
}

export async function callAction(forUrl, forUser, keyChatRoom, isvideo = false) {
  try {
    //console.log((new Date()).toLocaleString(), " >= callAction", forUrl);

    if (peerConnectionArray.has(GetNameKey(forUrl, forUser))) {
      //console.log((new Date()).toLocaleString(), " >= changeOffer", forUrl);
      return await StartOfferForChangeStream(forUrl, forUser, keyChatRoom);
    }
    else {
      mediaStreamConstraints = {
        audio: true,
        video: isvideo
      };

      await createPeerConnection(forUrl, forUser, keyChatRoom);

      if (!peerConnectionArray.has(GetNameKey(forUrl, forUser))) {
        //console.log((new Date()).toLocaleString(), "error get peerConnection for", forUrl);
        return null;
      }

      let offerDescription = await peerConnectionArray.get(GetNameKey(forUrl, forUser)).createOffer(offerOptions);

      await peerConnectionArray.get(GetNameKey(forUrl, forUser)).setLocalDescription(offerDescription);

      return JSON.stringify(offerDescription);
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error callAction", e);
  }
  return null;
}

// Signaling calls this once an answer has arrived from other peer. Once
// setRemoteDescription is called, the addStream event trigger on the connection.
export async function processAnswer(forUrl, forUser, descriptionText) {
  try {
    //console.log((new Date()).toLocaleString(), " => processAnswer: peerConnection setRemoteDescription start.", forUrl ?? "error");

    if (!peerConnectionArray.has(GetNameKey(forUrl, forUser))) return;

    let description = JSON.parse(descriptionText);

    await peerConnectionArray.get(GetNameKey(forUrl, forUser)).setRemoteDescription(description);

    //console.log((new Date()).toLocaleString(), "peerConnection.setRemoteDescription(description) success", forUrl);
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error processAnswer", GetNameKey(forUrl, forUser), e);
  }
}

//Создаем ответ для подключения
export async function processOffer(forUrl, forUser, keyChatRoom, descriptionText, isvideo = false) {
  try {
    //console.log((new Date()).toLocaleString(), " >= processOffer", forUrl);
    if (peerConnectionArray.has(GetNameKey(forUrl, forUser))) {
      //console.log((new Date()).toLocaleString(), " >= changeAnswer", forUrl);
      return await StartAnswerForChangeStream(descriptionText, forUrl, keyChatRoom);
    }
    else {
      mediaStreamConstraints = {
        audio: true,
        video: isvideo
      };

      await createPeerConnection(forUrl, forUser, keyChatRoom);

      if (peerConnectionArray.has(GetNameKey(forUrl, forUser))) {
        let description = JSON.parse(descriptionText);
        await peerConnectionArray.get(GetNameKey(forUrl, forUser)).setRemoteDescription(description);
        let answer = await peerConnectionArray.get(GetNameKey(forUrl, forUser)).createAnswer();
        await peerConnectionArray.get(GetNameKey(forUrl, forUser)).setLocalDescription(answer);

        return JSON.stringify(answer);
      }
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error processOffer", GetNameKey(forUrl, forUser), e);
  }
  closePeerConnection(forUrl, forUser);
  return null;
}

export async function processCandidate(forUrl, forUser, candidateText) {
  try {
    //console.log((new Date()).toLocaleString(), " >= processCandidate: peerConnection addIceCandidate start.", forUrl ?? "error", ". Is find P2P ", peerConnectionArray.has(forUrl));
    if (!peerConnectionArray.has(GetNameKey(forUrl, forUser))) return;

    let candidate = JSON.parse(candidateText);
    await peerConnectionArray.get(GetNameKey(forUrl, forUser)).addIceCandidate(candidate);
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error processCandidate", GetNameKey(forUrl, forUser), e);
  }
}

// Handles hangup action: ends up call, closes connections and resets peers.
export function closePeerConnection(forUrl, forUser) {
  try {
    videoMixer.removeVideo(GetNameKey(forUrl, forUser));
    if (peerConnectionArray.has(GetNameKey(forUrl, forUser))) {
      peerConnectionArray.get(GetNameKey(forUrl, forUser)).close();
      peerConnectionArray.delete(GetNameKey(forUrl, forUser));
      //console.log((new Date()).toLocaleString(), forUrl, "Ending call. P2P active ", peerConnectionArray.size);
    }
    let remoteVideoArray = getRemoteArrayPlayer();
    if (remoteVideoArray) {
      //console.log((new Date()).toLocaleString(), "remove video for url ", forUrl);
      remoteVideoArray.querySelector('[for="' + GetNameKey(forUrl, forUser) + '"]')?.remove();

      if (remoteVideoArray.querySelectorAll("video").length == 0) {
        remoteVideoArray.classList.add("d-none");
      }
      else if (remoteVideoArray.querySelectorAll("video").length == 1) {
        remoteVideoArray.classList.remove("row-cols-2");
        remoteVideoArray.classList.remove("row-cols-3");
        remoteVideoArray.classList.add("row-cols-1");
      }
      else if (remoteVideoArray.querySelectorAll("video").length < 4) {
        remoteVideoArray.classList.remove("row-cols-1");
        remoteVideoArray.classList.remove("row-cols-3");
        remoteVideoArray.classList.add("row-cols-2");
      }
      else {
        remoteVideoArray.classList.remove("row-cols-1");
        remoteVideoArray.classList.remove("row-cols-2");
        remoteVideoArray.classList.add("row-cols-3");
      }
    }

    //если нет активных подключений, закрываем локальный стрим
    if (peerConnectionArray.size == 0 && localStream) {
      var localPlayer = getLocalPlayer();
      if (localPlayer) {
        localPlayer.srcObject = null;
        localPlayer.classList.add("d-none");
        localStream.getTracks().forEach(track => {
          track.stop();
          localStream.removeTrack(track);
        });
        screenAndCamMixer.StopMixed();
        localStream = null;
      }
      videoMixer.removeAllVideo();

      if (videoRecord) {
        videoRecord.stop();
        videoRecord = null;
      }
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error closePeerConnection", e);
  }
}

// Handles remote MediaStream success by handing the stream to the blazor component.
function gotRemoteMediaStream(event, forUrl, forUser, keyChatRoom) {
  try {
    let remoteVideoArray = getRemoteArrayPlayer();
    if (remoteVideoArray) {
      let remoteVideo = remoteVideoArray.querySelector('[for="' + GetNameKey(forUrl, forUser) + '"]');

      if (!remoteVideo) {
        remoteVideo = document.createElement("video");

        remoteVideo.autoplay = true;
        remoteVideo.poster = "/sound.svg";

        remoteVideo.classList.add("col");
        remoteVideo.classList.add("p-2");
        remoteVideo.setAttribute("for", GetNameKey(forUrl, forUser));
        remoteVideo.setAttribute("title", GetNameKey(forUrl, forUser));

        remoteVideoArray.append(remoteVideo);

        remoteVideoArray.classList.remove("d-none");

        if (remoteVideoArray.querySelectorAll("video").length > 10) {
          remoteVideoArray.classList.remove("row-cols-1");
          remoteVideoArray.classList.remove("row-cols-2");
          remoteVideoArray.classList.add("row-cols-3");
        }
        else if (remoteVideoArray.querySelectorAll("video").length > 1) {
          remoteVideoArray.classList.remove("row-cols-1");
          remoteVideoArray.classList.remove("row-cols-3");
          remoteVideoArray.classList.add("row-cols-2");
        }
        else if (remoteVideoArray.querySelectorAll("video").length == 1) {
          remoteVideoArray.classList.remove("row-cols-2");
          remoteVideoArray.classList.remove("row-cols-3");
          remoteVideoArray.classList.add("row-cols-1");
        }
        else {
          remoteVideoArray.classList.remove("row-cols-2");
          remoteVideoArray.classList.remove("row-cols-3");
          remoteVideoArray.classList.remove("row-cols-1");
          remoteVideoArray.classList.add("d-none");
        }
      }

      if (remoteVideo.srcObject) {
        remoteVideo.srcObject.getTracks().forEach(track => track.stop());
        remoteVideo.srcObject = null;
      }

      remoteVideo.srcObject = event.stream;
      //console.log("gotRemoteMediaStream", forUrl, forUser);
      videoMixer.addVideos(remoteVideo);

      let videoTrack = event.stream.getVideoTracks();

      if (videoTrack.length > 0) {
        SetEventForVideoTrack(forUrl, forUser, videoTrack[0]);
      }
      else {
        event.stream.addEventListener("addtrack", (e) => {
          //console.log((new Date()).toLocaleString(), "addtrack", event);
          if (e.track.kind == 'video') {
            SetEventForVideoTrack(forUrl, forUser, e.track);
          }
        });
      }
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error gotRemoteMediaStream", e);
  }
}

function SetEventForVideoTrack(forUrl, forUser, videoTrack) {
  let remoteVideoArray = getRemoteArrayPlayer();
  if (remoteVideoArray) {
    let videoElem = remoteVideoArray.querySelector("[for='" + GetNameKey(forUrl, forUser) + "']");
    if (videoElem) {
      if (videoTrack) {
        videoTrack.onmute = (event) => {
          try {
            videoElem.srcObject.removeTrack(event.currentTarget);
            videoElem.load();
            //console.log((new Date()).toLocaleString(), "Communication onmute", videoElem.srcObject.getTracks());
          }
          catch (e) {
            console.log((new Date()).toLocaleString(), "Communication error onmute", e);
          }
        };
        videoTrack.onunmute = (event) => {
          try {
            videoElem.srcObject.addTrack(event.currentTarget);
            //console.log((new Date()).toLocaleString(), "Communication onunmute", videoElem.srcObject.getTracks());
          }
          catch (e) {
            console.log((new Date()).toLocaleString(), "Communication error onunmute", e);
          }
        };
      }
    }
  }
}
// Sends candidates to peer through signaling.
async function handleConnection(event, forUrl, forUser, keyChatRoom) {
  try {
    const iceCandidate = event.candidate;
    if (iceCandidate) {
      //console.log((new Date()).toLocaleString(), " => SendCandidate for ", forUrl);
      await dotNet.invokeMethodAsync("SendCandidateJs", forUrl, forUser, keyChatRoom, JSON.stringify(iceCandidate));
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error handleConnection", e);
  }
}

// Logs changes to the connection state.
function handleConnectionChange(event, forUrl, forUser, keyChatRoom) {
  try {
    const peerConnection = event.target;
    //console.log((new Date()).toLocaleString(), `${forUrl} peerConnection ICE state: ${peerConnection.iceConnectionState}.`);

    if (peerConnection.iceConnectionState == 'disconnected' || peerConnection.iceConnectionState == 'closed') {
      dotNet.invokeMethodAsync("DisconnectP2P", forUrl, forUser, keyChatRoom);
    }
    else if (peerConnection.iceConnectionState == 'connected') {
      dotNet.invokeMethodAsync("ConnectedP2P", forUrl, forUser, keyChatRoom);
    }
  }
  catch (e) {
    console.log((new Date()).toLocaleString(), "Error handleConnectionChange", e);
  }
}

///////////////////////////////////////////////////////////////////////


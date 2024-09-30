export async function GetDirectiry() {
  try {
    var directoryHandle = await window.showDirectoryPicker();
    if (directoryHandle) {
      return directoryHandle.name.replace(/_+$/, '');
    }
  }
  catch (e) {
    console.error(e);
    return null;
  }
}

export async function GetDirectiryAndWriteKey() {
  try {
    var directoryHandle = await window.showDirectoryPicker({ mode: "readwrite" });
    if (directoryHandle) {
      var dirName = directoryHandle.name.replace(/_+$/, '');
      var has = await CreatePkoKey(dirName, new Date());

      const fileName = "pko.key";

      const fileHandle = await directoryHandle.getFileHandle(fileName, { create: true });

      const writable = await fileHandle.createWritable();

      await writable.write(has);

      await writable.close();
      return dirName;
    }
  }
  catch (e) {
    console.error(e);
    return null;
  }
}

async function CreatePkoKey(dirName, dateUtc) {
  let d = `${dateUtc.toISOString().slice(0, 16)}${dirName.replace(/_+$/, '')}`;
  var has = await ToSha256(d);
  return has;
}

async function ToSha256(message) {
  const msgBuffer = new TextEncoder().encode(message);
  const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
  return hashHex;
}

export async function GetDirectiryReference() {
  try {
    var directoryHandle = await window.showDirectoryPicker();
    if (directoryHandle) {
      var pkoKey = await directoryHandle.getFileHandle("pko.key");

      if (pkoKey) {
        var f = await pkoKey.getFile();
        var keyFile = await f.text();
        var key = await CreatePkoKey(directoryHandle.name.replace(/_+$/, ''), f.lastModifiedDate);
        if (keyFile == key) {
          return directoryHandle;
        }
      }
    }
  }
  catch (e) {
    console.error(e);
    return null;
  }
}

export async function ReadFilesForDirectiry(directoryHandle, fileName) {
  try {
    if (directoryHandle) {
      for await (const [key, value] of directoryHandle.entries()) {
        if (key == fileName) {
          var f = await value.getFile();
          return f;
        }
      }
    }
  }
  catch (e) {
    console.error(e);
    return null;
  }
}

export async function GetFilesForDirectiry(directoryHandle, dirName) {
  try {
    var list = [];
    if (directoryHandle && directoryHandle.name.replace(/_+$/, '') == dirName.replace(/_+$/, '')) {
      for await (const [key, value] of directoryHandle.entries()) {

        var ext = key.match(/(?:\.([^.]+))?$/)[0];

        if (ext == ".xml" || ext == ".csv" || ext == ".xlsx") {
          list.push(key);
        }
      }
      return list;
    }
  }
  catch (e) {
    console.error(e);
    return null;
  }
}
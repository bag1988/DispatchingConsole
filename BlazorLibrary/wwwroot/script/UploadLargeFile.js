
const xhrList = new Map();

var IsCancelled = false;

let dataTransferFromDrag = null;

export async function RegistrChangeValueForInput(queryElem, dotNet, callBackSendFiles, extensionsFileArray, maxCountFile) {
  try {
    dataTransferFromDrag = new DataTransfer();
    var inputFile = document.querySelector(queryElem);
    if (inputFile) {
      inputFile.onchange = async (event) => {
        try {
          dataTransferFromDrag = new DataTransfer();
          let fileList = GetFilesFromEvent(event, extensionsFileArray, maxCountFile);
          let sendFileListInfo = null;
          if (fileList) {
            sendFileListInfo = Array.prototype.map.call(fileList, function (file) {
              const result = {
                lastModified: new Date(file.lastModified).toISOString(),
                name: file.name,
                size: file.size,
                contentType: file.type
              }
              return result
            });
          }
          await dotNet.invokeMethodAsync(callBackSendFiles, sendFileListInfo, event.target.files.length > 0);

          for (var item of fileList) {
            dataTransferFromDrag.items.add(item);
          }
        }
        catch (e) {
          event.target.value = null;
          dataTransferFromDrag = new DataTransfer();
          console.error("Ошибка добавления файла", ex.message);
        }
      }
    }
  }
  catch (ex) {
    console.error("Ошибка подписки на изменение значения для input", ex.message);
  }
}

export async function StartUploadOneFileForElem(queryElem, apiUpload, dotNet, callBackProgress) {
  try {
    var inputFile = document.querySelector(queryElem);
    if (inputFile && inputFile.files?.length > 0) {
      return (await StartUploadOneFile(inputFile.files[0], apiUpload, dotNet, callBackProgress));
    }
    else {
      console.error("element don`t type 'file'");
    }
  }
  catch (ex) {
    console.error(ex.message);
  }
}

export async function StartUploadOneFile(fileUpload, apiUpload, dotNet, callBackProgress) {
  try {
    if (fileUpload) {
      AbortUploadForFileName(fileUpload.name);
      if (!xhrList.has(fileUpload.name)) {
        const xhr = new XMLHttpRequest();
        xhrList.set(fileUpload.name, xhr);
        const statusCode = await new Promise((resolve) => {
          try {
            xhr.upload.onprogress = async (event) => {
              if (event.lengthComputable) {
                await dotNet.invokeMethodAsync(callBackProgress, event.loaded, event.total, fileUpload.name);
              }
            };
            xhr.onprogress = async (event) => {
              if (event.lengthComputable) {
                await dotNet.invokeMethodAsync(callBackProgress, event.loaded, event.total, fileUpload.name);
              }
            };
            xhr.onloadend = () => {
              resolve(xhr.status);
            };
            xhr.open("POST", apiUpload, true);
            //xhr.setRequestHeader("Content-Disposition", `attachment; filename=${fileUpload.name}`);
            //xhr.setRequestHeader("Content-Type", "application/octet-stream");
            //xhr.setRequestHeader("cache-control", "no-store, must-revalidate, private");
            // Build the payload
            const fileData = new FormData();
            fileData.append("file", fileUpload);
            xhr.send(fileData);
          }
          catch (error) {
            console.log("Ошибка отправки файла", error.message);
          }          
        });
        AbortUploadForFileName(fileUpload.name);
        return statusCode;
      }
    }
    else {
      console.error("No file for upload");
    }
  }
  catch (ex) {
    console.error(ex.message);
  }
  return 400;
}

export async function RegisterDragAndDropForElemAutoUpload(queryElem, apiUpload, dotNet, callBackProgress, extensionsFileArray, maxCountFile) {
  try {
    var elemForDrag = document.querySelector(queryElem);
    if (elemForDrag) {
      elemForDrag.ondrop = async (event) => {
        try {
          let fileList = GetFilesFromEvent(event, extensionsFileArray, maxCountFile);
          if (fileList) {
            for (let fileItem of fileList) {
              await StartUploadOneFile(fileItem, apiUpload, dotNet, callBackProgress)
            }
          }
        }
        catch (e) {
          console.error("Ошибка загрузки файла", e.message);
        }
        event.target.removeAttribute("drop-active");
      };
      elemForDrag.ondragover = (event) => {
        event.target.setAttribute("drop-active", true);
        event.preventDefault();
      };
      elemForDrag.ondragleave = (event) => {
        event.target.removeAttribute("drop-active");
      };
    }
  }
  catch (ex) {
    console.error("Error register drag and drop", ex.message);
  }
}

export async function RegisterDragAndDropForElemManualUpload(queryElem, dotNet, callBackSendFiles, extensionsFileArray, maxCountFile) {
  try {
    dataTransferFromDrag = new DataTransfer();
    var elemForDrag = document.querySelector(queryElem);
    if (elemForDrag) {
      elemForDrag.ondrop = async (event) => {
        try {
          dataTransferFromDrag = new DataTransfer();
          let fileList = GetFilesFromEvent(event, extensionsFileArray, maxCountFile);
          let sendFileListInfo = null;
          if (fileList) {
            sendFileListInfo = Array.prototype.map.call(fileList, function (file) {
              const result = {
                lastModified: new Date(file.lastModified).toISOString(),
                name: file.name,
                size: file.size,
                contentType: file.type
              }
              return result
            });
          }
          await dotNet.invokeMethodAsync(callBackSendFiles, sendFileListInfo, true);

          for (var item of fileList) {
            dataTransferFromDrag.items.add(item);
          }
        }
        catch (e) {
          dataTransferFromDrag = new DataTransfer();
          console.error("Ошибка добавления файлов для загрузки", e.message);
        }
        event.target.removeAttribute("drop-active");
      };
      elemForDrag.ondragover = (event) => {
        event.target.setAttribute("drop-active", true);
        event.preventDefault();
      };
      elemForDrag.ondragleave = (event) => {
        event.target.removeAttribute("drop-active");
      };
    }
  }
  catch (ex) {
    console.error("Error register drag and drop", ex.message);
  }
}
function GetFilesFromEvent(event, extensionsFileArray, maxCountFile) {
  try {
    const ev = event.clipboardData ?? event.dataTransfer ?? event.target;
    console.debug("File(s) dropped");
    const dataTransfer = new DataTransfer();
    if (ev.files.length > 0) {
      event.preventDefault();
      for (var item of [...ev.files].filter((i) => (extensionsFileArray?.includes(/^.+(\.[^.]+)$/.exec(i.name)?.at(1)) ?? true)).slice(0, (maxCountFile ?? 50))) {
        dataTransfer.items.add(item);
      }
    } else {
      for (var item of [...ev.items].filter((i) => i.kind === "file" && (extensionsFileArray?.includes(/^.+(\.[^.]+)$/.exec(i.name)?.at(1)) ?? true)).slice(0, (maxCountFile ?? 50))) {
        dataTransfer.items.add(item);
      }
    }
    return dataTransfer.files;
  }
  catch (ex) {
    console.error("Ошибка получения списка файлов", ex.message);
  }
  return null;
}

export async function UploadManualDragFiles(apiUpload, dotNet, callBackProgress, callBackStatusCode) {
  try {
    IsCancelled = false;
    for (let fileItem of dataTransferFromDrag.files) {
      if (IsCancelled) {
        console.debug("Загрузка файлов прервана");
        break;
      }
      let result = await StartUploadOneFile(fileItem, apiUpload, dotNet, callBackProgress);
      await dotNet.invokeMethodAsync(callBackStatusCode, fileItem.name, result);
    }
  }
  catch (ex) {
    console.error("Ошибка загрузки файлов", ex.message);
  }
  dataTransferFromDrag = new DataTransfer();
}

export function AbortAll() {
  try {
    IsCancelled = true;    
    for (const [key, value] of xhrList) {
      value.abort();
      xhrList.delete(key)
    }
    dataTransferFromDrag = new DataTransfer();
  }
  catch (ex) {
    console.error("Error abort upload file", ex.message);
  }
}

export function AbortUploadForFileName(fileName) {
  try {
    if (xhrList.has(fileName)) {
      xhrList.get(fileName).abort();
      xhrList.delete(fileName);
    }
  }
  catch (ex) {
    console.error("Error abort upload file", ex.message);
  }
}
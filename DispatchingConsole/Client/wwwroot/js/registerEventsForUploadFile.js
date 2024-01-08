export function registerEventsForUploadFile(browserEventName, nameEvent) {
  if (Blazor) {
    Blazor.registerCustomEventType(nameEvent, {
      browserEventName: browserEventName,
      createEventArgs: event => {
        event.preventDefault();
        const ev = event.clipboardData ?? event.dataTransfer;
        if (ev) {
          let fileList;
          if (ev.files) {
            fileList = Array.prototype.map.call(ev.files, function (file) {
              const result = {
                lastModified: new Date(file.lastModified).toISOString(),
                name: file.name,
                size: file.size,
                contentType: file.type,
                stream: DotNet.createJSStreamReference(file)
              }
              return result
            });
          }
          else if (ev.items) {
            fileList = Array.prototype.map.call(ev.items, function (item) {
              if (item.kind === "file") {
                let file = item.getAsFile();
                const result = {
                  lastModified: new Date(file.lastModified).toISOString(),
                  name: file.name,
                  size: file.size,
                  contentType: file.type,
                  stream: DotNet.createJSStreamReference(file)
                }
                return result
              }
            });
          }

          let result = {
            Files: fileList
          };
          return result;
        }        
      }
    });
  }
}

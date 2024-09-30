export function RegisterEventsForDragAndDrop(browserEventName, nameEvent) {
  if (Blazor) {
    Blazor.registerCustomEventType(nameEvent, {
      browserEventName: browserEventName,
      createEventArgs: event => {
        const ev = event.clipboardData ?? event.dataTransfer;
        if (ev) {
          let fileList;
          if (ev.files.length > 0) {
            event.preventDefault();
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
            fileList = Array.prototype.map.call([...ev.items].filter((i) => i.kind === "file"), function (item) {
              let file = item.getAsFile();
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
          let result = {
            Files: fileList
          };
          return result;
        }
      }
    });
  }
}

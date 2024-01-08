using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DispatchingConsole.Client.Shared
{
    public class ICustomBrowserFile
    {
        public string? Name { get; set; }
        public long Size { get; set; } = 0;
        public DateTime? LastModified { get; set; }
        public string? ContentType { get; set; }
        public IJSStreamReference? Stream { get; set; }
    }

    public class OnSetFilesEventArgs : EventArgs
    {
        public IReadOnlyList<ICustomBrowserFile>? Files { get; set; }
    }
}

namespace DispatchingConsole.Client.Shared.DropFiles
{
    [EventHandler("ondropfiles", typeof(OnSetFilesEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {

    }
}


namespace DispatchingConsole.Client.Shared.PasteFiles
{
    [EventHandler("onpastefiles", typeof(OnSetFilesEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {

    }
}

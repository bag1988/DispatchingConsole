using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.Models.CustomEvents.DropFiles
{
    [EventHandler("ondropfiles", typeof(OnSetFilesEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {

    }
}

namespace BlazorLibrary.Models.CustomEvents.PasteFiles
{
    [EventHandler("onpastefiles", typeof(OnSetFilesEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {

    }
}

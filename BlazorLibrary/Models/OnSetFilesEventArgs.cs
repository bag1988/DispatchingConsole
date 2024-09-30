namespace BlazorLibrary.Models
{
    public class OnSetFilesEventArgs : EventArgs
    {
        public IReadOnlyList<ICustomBrowserFile>? Files { get; set; }
    }
}

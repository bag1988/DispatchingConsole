using LocalizationLibrary;
using Microsoft.JSInterop;

namespace BlazorLibrary.Models
{
    public class ICustomBrowserFile : ICustomBrowserFileInfo
    {
        public IJSStreamReference? Stream { get; set; }
    }


    public class ICustomBrowserFileInfo
    {
        public string? Name { get; set; }
        public long Size { get; set; } = 0;
        public DateTime? LastModified { get; set; }
        public string? ContentType { get; set; }
    }
    public class FileUploadInfo : ICustomBrowserFileInfo
    {
        public long Loaded { get; set; } = 0;
        public DateTime? StartUpload { get; set; }

        public int? StatusCode { get; set; }

        public DateTime? EndUpload { get; set; }

        public TimeSpan GetUploadTime
        {
            get
            {
                return (EndUpload ?? DateTime.Now) - (StartUpload ?? DateTime.Now);
            }
        }

        public int GetTotalProgressProcent
        {
            get
            {
                if (Size > 0 && Loaded > 0)
                {
                    return (int)(Math.Ceiling(((double)Loaded / (Size / 1024)) * 100));
                }
                return 0;
            }
        }

        public TimeSpan GetRemainingTime
        {
            get
            {
                return TimeSpan.FromSeconds(Math.Ceiling(((Size / 1024) - Loaded) / (GetSpeedUpload * 1024)));
            }
        }

        public double GetSpeedUpload
        {
            get
            {
                return ((Loaded) / GetUploadTime.TotalSeconds / 1_000);
            }
        }
    }
}

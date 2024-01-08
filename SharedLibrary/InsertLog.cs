using System.Diagnostics;

namespace SharedLibrary
{
    public class InsertLog
    {
        public static async Task AddParam(string? JsonParam = null)
        {
            await Task.Run(() =>
            {
                var activity = Activity.Current;
                activity?.SetTag("operation.param", JsonParam);
                activity?.Dispose();
            });
        }

        public static Activity? GetCurrent()
        {
            return Activity.Current;
        }

        public static async Task SetNewLog(string NameLog, string? JsonParam = null)
        {
            await Task.Run(() =>
            {
                using var activity = Activity.Current?.Source.StartActivity(NameLog);
                activity?.SetTag("operation.param", JsonParam);
                activity?.Dispose();
            });
        }
    }
}

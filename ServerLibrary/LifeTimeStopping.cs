using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using ServerLibrary.HubsProvider;
using SharedLibrary;
namespace ServerLibrary
{
    public static class LifeTimeStopping
    {
        public static void SendAllLogout(object? hubContext)
        {
            try
            {
                if (hubContext is IHubContext<SharedHub>)
                {
                    ((IHubContext<SharedHub>)hubContext).Clients.All.SendAsync(nameof(DaprMessage.Fire_AllUserLogout), "");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void SendAllRestartUi(object? hubContext)
        {
            try
            {
                if (hubContext is IHubContext<SharedHub>)
                {
                    ((IHubContext<SharedHub>)hubContext).Clients.All.SendAsync(nameof(DaprMessage.Fire_RestartUi), "");
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

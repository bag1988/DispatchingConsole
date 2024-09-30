using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorLibrary.Helpers
{
    public static class SubscibeHubExtensions
    {
        public static void SubscribeViaInterface<TData>(this HubConnection hubConnection, TData pages, Type interfaceType)
        {
            if (pages == null)
                return;

            var interfaceMap = typeof(TData).GetInterfaceMap(interfaceType);

            foreach (var hubAction in interfaceMap.TargetMethods)
            {
                if (!string.IsNullOrEmpty(hubAction.Name))
                {
                    if (hubAction.GetParameters().Any())
                    {
                        hubConnection.On(hubAction.Name, hubAction.GetParameters().Select(p => p.ParameterType).ToArray(), (Value) =>
                        {
                            try
                            {
                                hubAction.Invoke(pages, Value);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                            return Task.CompletedTask;
                        });
                    }
                    else
                    {
                        hubConnection.On(hubAction.Name, () =>
                        {
                            try
                            {
                                hubAction.Invoke(pages, null);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            return Task.CompletedTask;
                        });
                    }
                }
            }
        }
    }
}

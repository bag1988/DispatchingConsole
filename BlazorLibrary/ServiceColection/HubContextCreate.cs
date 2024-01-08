using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorLibrary.ServiceColection
{
    public class HubContextCreate : IAsyncDisposable
    {
        readonly Uri urlSignal;

        readonly HubConnection hubConnection;

        readonly Dictionary<string, List<string>> handlerList = new();

        public HubContextCreate(NavigationManager _NavigationManager)
        {
            urlSignal = _NavigationManager.ToAbsoluteUri("/CommunicationHub");
            hubConnection = new HubConnectionBuilder().WithUrl(urlSignal).WithAutomaticReconnect(new RRetryPolicy()).Build();
        }

        public HubContextCreate(NavigationManager _NavigationManager, string urlHub)
        {
            urlSignal = _NavigationManager.ToAbsoluteUri($"/{urlHub}");
            hubConnection = new HubConnectionBuilder().WithUrl(urlSignal).WithAutomaticReconnect().Build();
        }

        public HubContextCreate(NavigationManager _NavigationManager, string urlHub, KeyValuePair<string, string> header)
        {
            urlSignal = _NavigationManager.ToAbsoluteUri($"/{urlHub}");
            hubConnection = new HubConnectionBuilder().WithUrl(urlSignal,
                options => options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.None).WithAutomaticReconnect().Build();
        }

        public HubContextCreate(NavigationManager _NavigationManager, string urlHub, Dictionary<string, string> headers)
        {
            urlSignal = _NavigationManager.ToAbsoluteUri($"/{urlHub}");
            hubConnection = new HubConnectionBuilder().WithUrl(urlSignal, options =>
            {
                options.Headers = (IDictionary<string, string>)options.Headers.Concat(headers);
            }).WithAutomaticReconnect().Build();
        }

        public async Task InitAsync(CancellationToken token = default)
        {
            if (hubConnection.State != HubConnectionState.Connected)
                await hubConnection.StartAsync(token);
        }

        public Task<System.Threading.Channels.ChannelReader<TResult>> StreamAsChannelCoreAsync<TResult>(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            if (hubConnection.State != HubConnectionState.Connected)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }
            return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, args, cancellationToken);
        }

        public IAsyncEnumerable<TResult> StreamAsyncCore<TResult>(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            if (hubConnection.State != HubConnectionState.Connected)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }
            return hubConnection.StreamAsyncCore<TResult>(methodName, args, cancellationToken);
        }

        public async Task SendCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            if (hubConnection.State != HubConnectionState.Connected)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }
            await hubConnection.SendCoreAsync(methodName, args, cancellationToken);
        }

        public Task<TResult> InvokeCoreAsync<TResult>(string methodName, CancellationToken cancellationToken = default)
        {
            if (hubConnection.State != HubConnectionState.Connected)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }
            return hubConnection.InvokeCoreAsync<TResult>(methodName, Array.Empty<object>(), cancellationToken);
        }

        public Task InvokeCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            if (hubConnection.State != HubConnectionState.Connected)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }
            return hubConnection.InvokeCoreAsync(methodName, args, cancellationToken);
        }

        public Task<TResult> InvokeCoreAsync<TResult>(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            if (hubConnection.State != HubConnectionState.Connected)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.InvokeCoreAsync<TResult>(methodName, args, cancellationToken);
        }

        public async Task SubscribeAsync(object? Pages)
        {
            if (Pages == null)
                return;

            var ParamMethod = Pages.GetType().GetMethods().Where(x => x.GetCustomAttributes<DescriptionAttribute>().Any()).Select(x => new { method = x, name = x.Name, param = x.GetParameters().Select(p => p.ParameterType).ToArray() }).ToList();

            if (ParamMethod?.Any() ?? false)
            {
                var pageName = Pages.GetType().Name;

                foreach (var hubAction in ParamMethod)
                {
                    if (handlerList.ContainsKey(pageName) && handlerList[pageName].Contains(hubAction.name))
                    {
                        continue;
                    }

                    if (!handlerList.ContainsKey(pageName))
                    {
                        handlerList.Add(pageName, new());
                    }

                    if (!string.IsNullOrEmpty(hubAction.name))
                    {
                        handlerList[pageName].Add(hubAction.name);
                        if (hubAction.param?.Any() ?? false)
                        {
                            hubConnection.On(hubAction.name, hubAction.param, (Value) =>
                            {
                                try
                                {
                                    hubAction.method.Invoke(Pages, Value);
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
                            hubConnection.On(hubAction.name, () =>
                            {
                                try
                                {
                                    hubAction.method.Invoke(Pages, null);
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
            await StartConnect();
        }

        async Task StartConnect()
        {
            if (hubConnection.State != HubConnectionState.Connected && hubConnection.State != HubConnectionState.Connecting)
                await hubConnection.StartAsync();
        }

        public void SetFuncForReconnect(Func<string?, Task>? func)
        {
            hubConnection.Reconnected += func;
        }
               
        public HubConnectionState GetState
        {
            get
            {
                return hubConnection.State;
            }

        }

        public async ValueTask DisposeAsync()
        {
            if (hubConnection is not null)
            {
                SetFuncForReconnect(null);
                await hubConnection.DisposeAsync();
            }
        }
    }

    public class RRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            var delay = retryContext.PreviousRetryCount switch
            {
                < 1 => TimeSpan.FromSeconds(0),
                < 2 => TimeSpan.FromSeconds(2),
                < 3 => TimeSpan.FromSeconds(5),
                _ => TimeSpan.FromSeconds(10),
            };
            return delay;
        }
    }
}

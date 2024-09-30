using System.ComponentModel;
using System.Reflection;
using BlazorLibrary.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SharedLibrary.GlobalEnums;

namespace BlazorLibrary.ServiceColection
{
    public class HubContextCreate : IAsyncDisposable
    {
        readonly HubConnection hubConnection;

        readonly ILogger<HubContextCreate> _logger;
        public HubContextCreate(NavigationManager _NavigationManager, HttpClient httpClient, ILogger<HubContextCreate> logger)
        {
            _logger = logger;
            var urlSignal = _NavigationManager.ToAbsoluteUri($"/ExchangeHub?{nameof(CookieName.AppId)}={httpClient.DefaultRequestHeaders.GetHeader(nameof(CookieName.AppId))}");
            hubConnection = new HubConnectionBuilder().WithUrl(urlSignal).WithAutomaticReconnect(new RRetryPolicy()).Build();
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
                
        public async Task SubscribeAndStartAsync<TData>(TData pages, Type interfaceType)
        {
            if (pages == null)
                return;
            hubConnection.SubscribeViaInterface(pages, interfaceType);
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

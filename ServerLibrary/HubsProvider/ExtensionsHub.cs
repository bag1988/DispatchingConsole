using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SensorM.GsoCommon.ServerLibrary.HubsProvider;
using ServerLibrary.Extensions;
using SharedLibrary.Utilities;

namespace ServerLibrary.HubsProvider
{
    public class ExtensionsHub : Hub<IExtensionsHub>
    {
        private readonly ILogger<ExtensionsHub> _logger;
        public ExtensionsHub(ILogger<ExtensionsHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                var context = Context.GetHttpContext();
                var urlConnect = context?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1"; 

                _logger.LogTrace(@"Новое подключение расширения к хабу для {forUrl}, назначенный ID {Id}, локальный хост {Host}", urlConnect, Context.ConnectionId, context?.Request.Host);

                AddToGroup(urlConnect).Wait();

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(OnConnectedAsync));
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var context = Context.GetHttpContext();
                var urlConnect = context?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";

                _logger.LogTrace(@"Закрыто подключение расширения к хабу для {forUrl}, назначенный ID {Id}, локальный хост {Host}", urlConnect, Context.ConnectionId, context?.Request.Host);
                RemoveFromGroup(urlConnect).Wait();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(OnDisconnectedAsync));
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task AddToGroup(string groupName)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName.Replace(".", "_"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(AddToGroup));
            }
        }

        public async Task RemoveFromGroup(string groupName)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName.Replace(".", "_"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(RemoveFromGroup));
            }
        }

    }
}

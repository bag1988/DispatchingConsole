using System.Net.Sockets;
using SMDataServiceProto.V1;
using SharedLibrary.Models;
using Microsoft.AspNetCore.SignalR.Client;
using SharedLibrary.Utilities;

namespace DispatchingConsole.Server.BackgroundServices
{
    public class UpdateLocalUsers : BackgroundService
    {
        private readonly ILogger<UpdateLocalUsers> _logger;
        readonly HubConnection? _hubConnection;

        readonly int _port;
        readonly IServiceProvider _serviceProvider;

        public UpdateLocalUsers(ILogger<UpdateLocalUsers> logger, IConfiguration appConfiguration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            var url = appConfiguration.GetValue<string?>("Kestrel:Endpoints:Http:Url")?.Split(":").LastOrDefault() ?? "8080";

            if (int.TryParse(url, out _port))
            {
                _hubConnection = new HubConnectionBuilder().WithUrl(new Uri($"http://127.0.0.1:{_port}/CommunicationChatHub")).Build();
            }
            _serviceProvider = serviceProvider;
        }

        async Task SendHubConnect(string method, object?[]? args = null)
        {
            if (args == null)
                args = Array.Empty<object>();
            if (_hubConnection != null)
            {
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    await _hubConnection.StartAsync();
                }
                await _hubConnection.SendCoreAsync(method, args);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => _logger.LogTrace("UpdateLocalUsers is stopping."));
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await SendHubConnect("WriteLocalContact");

                    await UpdateRemoteUser(stoppingToken);

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            _logger.LogTrace("UpdateLocalUsers background task is stopping.");
        }


        async Task UpdateRemoteUser(CancellationToken stoppingToken)
        {
            try
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    StaffDataProto.V1.StaffData.StaffDataClient staffData = scope.ServiceProvider.GetRequiredService<StaffDataProto.V1.StaffData.StaffDataClient>();
                    var cuArray = await staffData.GetItems_IRegistrationAsync(new GetItemRequest() { ObjID = new() });

                    if (cuArray.Array?.Count > 0)
                    {
                        foreach (var item in cuArray.Array)
                        {
                            var unc = IpAddressUtilities.GetHost(item.CCURegistrList?.CUUNC);
                            if (!string.IsNullOrEmpty(unc))
                            {
                                var absoluteUri = $"http://{unc}:{_port}";
                                try
                                {
                                    if (stoppingToken.IsCancellationRequested)
                                    {
                                        return;
                                    }

                                    var handler = new HttpClientHandler();
                                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                                    handler.ServerCertificateCustomValidationCallback =
                                        (httpRequestMessage, cert, cetChain, policyErrors) =>
                                        {
                                            return true;
                                        };

                                    using (var httpClient = new HttpClient(handler))
                                    {
                                        httpClient.BaseAddress = new Uri(absoluteUri);

                                        var result = await httpClient.PostAsync("api/v1/chat/GetLocalContact", null, stoppingToken);

                                        if (result.IsSuccessStatusCode)
                                        {
                                            var listUser = await result.Content.ReadFromJsonAsync<IEnumerable<ContactInfo>?>(cancellationToken: stoppingToken);

                                            await SendHubConnect("WriteRemoteContact", [listUser]);
                                        }
                                    }

                                }
                                catch (Exception ex)
                                {
                                    _logger.LogTrace(ex.Message);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}

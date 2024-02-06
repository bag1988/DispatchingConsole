using SMDataServiceProto.V1;
using SharedLibrary.Models;
using Microsoft.AspNetCore.SignalR.Client;
using SharedLibrary.Utilities;
using ServerLibrary.Extensions;
using Grpc.Net.Client;
using System.Net.Sockets;
using SensorM.GsoCommon.ServerLibrary;
using System.Net.Http;

namespace DispatchingConsole.Server.BackgroundServices
{
    public class UpdateLocalUsers : BackgroundService
    {
        private readonly ILogger<UpdateLocalUsers> _logger;
        readonly HubConnection? _hubConnection;

        readonly int _port;
        readonly IServiceProvider _serviceProvider;
        readonly int _AppHttpsPort = 2291;

        private readonly static CreateHttpClient _httpClient = new();

        public UpdateLocalUsers(ILogger<UpdateLocalUsers> logger, IConfiguration appConfiguration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            var url = appConfiguration.GetValue<string?>("Kestrel:Endpoints:Http:Url")?.Split(":").LastOrDefault() ?? "8080";

            if (int.TryParse(url, out _port))
            {
                _hubConnection = new HubConnectionBuilder().WithUrl(new Uri($"http://127.0.0.1:{_port}/CommunicationChatHub")).Build();
            }
            _serviceProvider = serviceProvider;
            _AppHttpsPort = appConfiguration.GetValue<int?>("DISPATCHINGCONSOLE_APP_HTTPS_PORT") ?? _AppHttpsPort;
        }

        async Task SendHubConnect(string method, object?[]? args = null)
        {
            try
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
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendHubConnect));
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => _logger.LogTrace("UpdateLocalUsers is stopping."));
            try
            {
                _ = Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var currentMemory = GC.GetTotalMemory(false);

                        _logger.LogInformation("Объем GC памяти {0:N0}", currentMemory);

                        if (currentMemory > 500_000_000)
                        {
                            _logger.LogInformation("Запуск сборщика мусора: {time}", DateTimeOffset.Now);
                            GC.Collect(2);
                            GC.WaitForPendingFinalizers();
                            _logger.LogInformation("Объем GC памяти после очистки: {1:N0}", GC.GetTotalMemory(true));
                        }
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                }, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await SendHubConnect("WriteLocalContact");

                    await UpdateRemoteUser(stoppingToken);

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ExecuteAsync));
            }
            _logger.LogTrace("UpdateLocalUsers background task is stopping.");
        }


        async Task UpdateRemoteUser(CancellationToken stoppingToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            try
            {
                StaffDataProto.V1.StaffData.StaffDataClient staffData = scope.ServiceProvider.GetRequiredService<StaffDataProto.V1.StaffData.StaffDataClient>();
                var cuArray = await staffData.GetItems_IRegistrationAsync(new GetItemRequest() { ObjID = new() });

                if (cuArray.Array?.Count > 0)
                {
                    List<string> currentCuList = new();
                    foreach (var item in cuArray.Array)
                    {
                        var unc = IpAddressUtilities.GetHost(item.CCURegistrList?.CUUNC);
                        
                        if (!string.IsNullOrEmpty(unc))
                        {
                            var absoluteUri = $"https://{unc}:{_AppHttpsPort}";

                            if (stoppingToken.IsCancellationRequested)
                            {
                                return;
                            }

                            try
                            {
                                var client = _httpClient.GetHttpClient(absoluteUri);

                                if (client != null)
                                {
                                    var result = await client.PostAsync("api/v1/chat/GetLocalContact", null, stoppingToken);

                                    if (result.IsSuccessStatusCode)
                                    {
                                        var listUser = await result.Content.ReadFromJsonAsync<IEnumerable<ContactInfo>?>(cancellationToken: stoppingToken);

                                        await SendHubConnect("WriteRemoteContact", [listUser]);

                                        currentCuList.Add($"{unc}:{_AppHttpsPort}");
                                    }
                                }
                                else
                                {
                                    _logger.LogTrace("{url} ошибка подключения", absoluteUri);
                                }
                            }
                            catch (Exception ex)
                            {
                                _httpClient.RemoveClient(absoluteUri);
                                _logger.LogTrace("{url} ошибка получения данных: {message}", absoluteUri, ex.Message);
                            }
                        }
                    }
                    if (currentCuList.Count > 0)
                    {
                        await SendHubConnect("SetCurrentRemoteContactForIp", [currentCuList]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(UpdateRemoteUser));
            }
            scope.Dispose();
        }
    }
}

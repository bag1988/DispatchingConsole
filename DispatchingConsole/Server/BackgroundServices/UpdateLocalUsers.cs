using SMDataServiceProto.V1;
using SharedLibrary.Models;
using Microsoft.AspNetCore.SignalR.Client;
using SharedLibrary.Utilities;
using ServerLibrary.Extensions;
using SensorM.GsoCore.RemoteConnectLibrary;
using SensorM.GsoCore.SharedLibrary.Interfaces;
using BlazorLibrary.Helpers;

namespace DispatchingConsole.Server.BackgroundServices
{
    public class UpdateLocalUsers : BackgroundService, IChatHub
    {
        private readonly ILogger<UpdateLocalUsers> _logger;
        readonly HubConnection? _hubConnection;

        class UrlErrorConnect(string url)
        {
            public string Url { get; } = url;
            public int CountError { get; set; } = 0;
            public DateTime LastError { get; set; } = DateTime.Now;

            public void AddError()
            {
                CountError++;
                LastError = DateTime.Now;
            }
            public void ResetError()
            {
                CountError = 0;
                LastError = DateTime.Now;
            }
        }

        readonly List<UrlErrorConnect> ErrorInfo = new();

        readonly int _port;
        readonly IServiceProvider _serviceProvider;
        public UpdateLocalUsers(ILogger<UpdateLocalUsers> logger, IConfiguration appConfiguration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            var url = appConfiguration.GetValue<string?>("Kestrel:Endpoints:Http:Url")?.Split(":").LastOrDefault() ?? "8080";

            if (int.TryParse(url, out _port))
            {
                _hubConnection = new HubConnectionBuilder().WithUrl(new Uri($"http://127.0.0.1:{_port}/CommunicationChatHub")).Build();
                _hubConnection.SubscribeViaInterface(this, typeof(IChatHub));
            }
            _serviceProvider = serviceProvider;
        }

        async Task SendHubConnect(string method, object?[]? args = null)
        {
            try
            {
                if (args == null)
                    args = Array.Empty<object>();
                if (_hubConnection != null)
                {
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StartAsync();
                    }

                    _logger.LogTrace("Вызов метода: {method}, состояние подключения к хабу: {state}", method, _hubConnection.State);
                    if (_hubConnection.State == HubConnectionState.Connected)
                    {
                        await _hubConnection.SendCoreAsync(method, args);
                    }
                    else
                    {
                        _logger.LogTrace("Ошибка пересылки данных в hub, состояние подключения {state}", _hubConnection.State);
                    }
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
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

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
                var staffData = scope.ServiceProvider.GetRequiredService<StaffDataProto.V1.StaffData.StaffDataClient>();
                var _httpClient = scope.ServiceProvider.GetRequiredService<RemoteHttpProvider>();
                var cuArray = await staffData.GetItems_IRegistrationAsync(new GetItemRequest() { ObjID = new() });

                var sMSSGso = scope.ServiceProvider.GetRequiredService<SMSSGsoProto.V1.SMSSGso.SMSSGsoClient>();

                var appPortInfo = await sMSSGso.GetAppPortsAsync(new Google.Protobuf.WellKnownTypes.BoolValue() { Value = true });

                if (cuArray.Array?.Count > 0 && _httpClient != null && appPortInfo != null)
                {
                    List<string> currentCuList = new();
                    foreach (var item in cuArray.Array)
                    {
                        var unc = IpAddressUtilities.GetHost(item.CCURegistrList?.CUUNC);

                        if (!string.IsNullOrEmpty(unc))
                        {
                            var absoluteUri = $"https://{unc}:{appPortInfo.DISPATCHINGCONSOLEAPPPORT}";

                            if (stoppingToken.IsCancellationRequested)
                            {
                                return;
                            }

                            if (!ErrorInfo.Any(x => x.Url == absoluteUri))
                            {
                                ErrorInfo.Add(new UrlErrorConnect(absoluteUri));
                            }
                            var errorInfo = ErrorInfo.First(x => x.Url == absoluteUri);

                            if (errorInfo.CountError > 2)
                            {
                                if (DateTime.Now.CompareTo(errorInfo.LastError.AddDays(1)) > 0)
                                {
                                    errorInfo.ResetError();
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            try
                            {

                                using var result = await _httpClient.PostAsync(absoluteUri, "api/v1/chat/GetLocalContact", null, HttpCompletionOption.ResponseHeadersRead, stoppingToken);
                                if (result.IsSuccessStatusCode)
                                {
                                    var listUser = await result.Content.ReadFromJsonAsync<IEnumerable<ContactInfo>?>(cancellationToken: stoppingToken);

                                    _logger.LogTrace("Обновление состояния пользователей от: {remote}, кол-во пользователей {count}", unc, listUser?.Count() ?? 0);
                                    await SendHubConnect("WriteRemoteContact", [listUser]);
                                    currentCuList.Add($"{unc}:{appPortInfo.DISPATCHINGCONSOLEAPPPORT}");

                                    errorInfo.ResetError();
                                }
                                else
                                {
                                    _logger.LogTrace("{url} получение списка пользователей завершилась ошибкой", absoluteUri);
                                    errorInfo.AddError();
                                }

                            }
                            catch (Exception ex)
                            {
                                _logger.LogTrace("{url} ошибка получения данных: {message}", absoluteUri, ex.Message);
                                errorInfo.AddError();
                            }

                            if (errorInfo.CountError > 2)
                            {
                                _logger.LogTrace("Для адреса {url}, превышено кол-во ошибок, опрос пользователей будет возобнавлен {date}", absoluteUri, errorInfo.LastError.AddDays(1));
                            }
                        }
                    }
                    if (currentCuList.Count > 0)
                    {
                        await SendHubConnect("SetCurrentRemoteContactForIp", [currentCuList]);
                    }
                }
                else if (_httpClient == null)
                {
                    _logger.LogTrace("Ошибка создания HttpClient");
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

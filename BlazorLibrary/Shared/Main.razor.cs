using System.Net.Http.Json;
using System.Timers;
using BlazorLibrary.Helpers;
using BlazorLibrary.Shared.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Routing;
using SMDataServiceProto.V1;
using Google.Protobuf.WellKnownTypes;
using SensorM.GsoCore.SharedLibrary;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorLibrary.Shared
{
    partial class Main : IAsyncDisposable
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public RenderFragment? Menu { get; set; }

        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public int? Width { get; set; } = 250;

        public ConfigStart ConfStart = new();

        AppPorts AppPortsInfo { get; set; } = new();

        int SystemId => ParseUrlSegments.GetSystemId(MyNavigationManager.Uri);

        public static MessageViewList? MessageView = default!;

        bool isPageLoad = false;

        readonly System.Timers.Timer timer = new(TimeSpan.FromMinutes(1));

        public static uint MinTimeoutLoad = 10;

        private bool _newVersionAvailable = false;
        private bool _browserIsFoundNewVersionAvailable = false;

        bool IsUpdateServiceWorker = false;

        DateTime sendLastActive = DateTime.Now;

        private ProductVersion? PVersion = null;

        private bool CollapseNavMenu = true;

        const int MaxNumberAttempsUpdate = 20;
        int NumberAttempsUpdate = MaxNumberAttempsUpdate;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await CheckUpdate();
                //init hub
                await InitAuthHubContext();

                await GetTimeLoadConnections();

                //проверка авторизации приложения
                await CheckAuthenticationApp();

                //настройки разрешенных подсистем
                ConfStart = await GetConfStart();

                //проверка активной подсистемы
                CheckSubSystemId();

                //проверка входящей строки
                await CheckQuery();

                StateHasChanged();

                MyNavigationManager.LocationChanged += MyNavigationManager_LocationChanged;

                await JSRuntime.InvokeVoidAsync("HotKeys.ListenWindowKey");

                if (timer != null)
                {
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();
                }
                isPageLoad = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка инициализации, {message}", ex.Message);
            }
        }

        private void ToggleNavMenu()
        {
            CollapseNavMenu = !CollapseNavMenu;
        }

        async Task CheckUpdateServiceWorker()
        {
            try
            {
                if (!_newVersionAvailable || !_browserIsFoundNewVersionAvailable)
                {
                    _browserIsFoundNewVersionAvailable = await JSRuntime.InvokeAsync<bool>("checkUpdateServiceWorker");
                    //_logger.LogTrace("Получен ответ на проверку наличия обновления, есть обновление {result}", _browserIsFoundNewVersionAvailable);
                    if (_browserIsFoundNewVersionAvailable)
                    {
                        _logger.LogTrace("Отображаем кнопку обновления");
                        _newVersionAvailable = true;
                        StateHasChanged();
                    }
                    else
                    {
                        TimeSpan delay = NumberAttempsUpdate switch
                        {
                            > 17 => TimeSpan.FromSeconds(2),
                            > 13 => TimeSpan.FromSeconds(5),
                            > 8 => TimeSpan.FromSeconds(10),
                            _ => TimeSpan.FromSeconds(30)
                        };

                        if (NumberAttempsUpdate < 10 && _authHubContext != null && _authHubContext.State != HubConnectionState.Connected)
                        {
                            NumberAttempsUpdate = MaxNumberAttempsUpdate;
                            return;
                        }

                        await Task.Delay(delay);
                        NumberAttempsUpdate--;
                        if (NumberAttempsUpdate == 0)
                        {
                            NumberAttempsUpdate = MaxNumberAttempsUpdate;
                            return;
                        }
                        _logger.LogTrace("Попытка № {value} проверки обновления", MaxNumberAttempsUpdate - NumberAttempsUpdate);
                        _ = CheckUpdateServiceWorker();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка проверки обновления {message}", ex.Message);
            }
        }

        private async Task PVersionFull()
        {
            try
            {
                var result = await Http.PostAsync("api/v1/allow/PVersionFull", null);
                if (result.IsSuccessStatusCode)
                {
                    PVersion = await result.Content.ReadFromJsonAsync<ProductVersion>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async Task<bool> CheckUpdate()
        {
            await PVersionFull();
            if (_authHubContext?.State == HubConnectionState.Connected && PVersion?.BuildNumber != GetVersionUi)
            {
                _logger.LogTrace("Серверная и клиентская версия отличается, отображаем кнопку обновления");
                _newVersionAvailable = true;
                StateHasChanged();
                return true;
            }
            return false;
        }


        string? GetVersionUi
        {
            get
            {
                return AssemblyNames.GetVersionPKO;
            }
        }


        public async Task OnFullRefreshPage()
        {
            _logger.LogTrace("Получена команда на полную перезагрузку страницы");
            timer.Stop();
            await JSRuntime.InvokeVoidAsync("window.location.reload");
            IsUpdateServiceWorker = false;
        }

        async Task UpdateServiceWorker()
        {
            IsUpdateServiceWorker = true;
            try
            {
                var countAttempts = 1;

                while (countAttempts < 4)
                {
                    var result = await JSRuntime.InvokeAsync<bool>("sendSkipWaitingServiceWorker");
                    if (!result)
                    {
                        if (new Uri(MyNavigationManager.Uri).Scheme == "http")
                        {
                            MyNavigationManager.Refresh(true);
                            return;
                        }
                        _logger.LogTrace($"Ошибка обновления, попытка № {countAttempts}");
                        countAttempts++;
                        await JSRuntime.InvokeAsync<bool>("checkUpdateServiceWorker");
                        await Task.Delay(1000);
                    }
                    else
                    {
                        return;
                    }
                }
                var unRegister = await JSRuntime.InvokeAsync<bool>("unregisterServiceWorker");
                if (unRegister)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка пропуска ожидания обновления {message}", ex.Message);
            }
            MessageView?.AddError("", GsoRep["ERROR_UPDATE_UI"]);
        }

        private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                if (_authHubContext?.State == HubConnectionState.Connected)
                {
                    await RefreshToken();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка проверки необходимости обновления токена, {message}", ex.Message);
            }
            timer.Start();
        }

        private void MyNavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
        {
            CheckSubSystemId();
            if (!CollapseNavMenu)
            {
                CollapseNavMenu = true;
                StateHasChanged();
            }
        }

        private async Task CheckAuthenticationApp()
        {
            try
            {
                var s = await _localStorage.GetTokenAsync();
                _logger.LogTrace("Проверка наличия токена");

                if (string.IsNullOrEmpty(s))
                {
                    _logger.LogTrace("Токен не установлен");
                }

                _logger.LogTrace("Отправка запроса на проверку авторизации приложения");

                var result = await InvokeToAuthHub<string?>("CheckAuthenticationApp", [s]);

                if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(s))
                {
                    _logger.LogTrace("Токен не актуален, выход пользователя");
                    await LogoutUser();
                }
                else if (!string.IsNullOrEmpty(result))
                {
                    _logger.LogTrace("Приложение авторизовано, обновляем токен");
                    await SetTokenAsync(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка проверки пользователя, {message}", ex.Message);
            }
        }

        async Task RefreshToken()
        {
            try
            {
                string userName = "";
                _logger.LogTrace("Проверка необходимости обновления токена");
                userName = await IsNeedRefreshToken() ?? "";
                if (!string.IsNullOrEmpty(userName))
                {
                    await SendRefreshToken(userName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка проверки токена, {message}", ex.Message);
            }
        }

        async Task<string?> IsNeedRefreshToken()
        {
            var user = (await _authStateProvider.GetAuthenticationStateAsync()).User;
            _logger.LogTrace("Пользователь {user}", user?.Identity?.Name ?? "не авторизован");
            if (user != null && !string.IsNullOrEmpty(user.Identity?.Name))
            {
                var exp = user.FindFirst(c => c.Type.Equals("exp"))?.Value;
                var expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));

                var now = DateTimeOffset.UtcNow;
                if (expTime.AddMinutes(-10).CompareTo(now) < 0)
                {
                    _logger.LogTrace("Истекло время действия токена для пользователя {user}", user.Identity.Name);
                    return user.Identity.Name;
                }
            }
            return null;
        }

        async Task OnSetActive()
        {
            if (sendLastActive.AddSeconds(20).CompareTo(DateTime.Now) <= 0)
            {
                _logger.LogTrace("SetLastActive {value}", sendLastActive);
                sendLastActive = DateTime.Now;
                if (await _User.GetName() != null)
                {
                    await SendToAuthHub("SetLastActive");
                }
            }
        }

        private async Task<ConfigStart> GetConfStart()
        {
            try
            {
                var result = await Http.PostAsync("api/v1/allow/GetConfStart", null);
                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadFromJsonAsync<ConfigStart>() ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return new();
        }

        async Task CheckQuery()
        {
            try
            {
                if (MyNavigationManager.Uri.Contains("systemId=") || MyNavigationManager.Uri.Contains("user=") || MyNavigationManager.Uri.Contains("token="))
                {
                    var _url = new Uri(MyNavigationManager.Uri);

                    var queryList = HttpUtility.ParseQueryString(_url.Query);
                    var navigateTo = ParseUrlSegments.AbsolutePath(MyNavigationManager.Uri);
                    if (queryList?.Count > 0)
                    {
                        var remoteUser = queryList.Get("user");

                        if (!string.IsNullOrEmpty(remoteUser))
                        {
                            var currentUser = await _User.GetName();

                            if (remoteUser != currentUser)
                            {
                                _logger.LogTrace("Удаленный пользователь {remote} не соответствует текущему {current}", remoteUser, currentUser);
                                if (!string.IsNullOrEmpty(currentUser))
                                {
                                    _logger.LogTrace("Выход приложения");
                                    await LogoutUser();
                                    await Task.Delay(1000);
                                }
                                var remotePassword = queryList.Get("password");

                                if (!string.IsNullOrEmpty(remoteUser))
                                {
                                    _logger.LogTrace("Авторизация удаленного пользователя");
                                    await AuthorizeRemoteUser(new() { User = remoteUser, Password = remotePassword });
                                }
                            }
                        }

                        var token = queryList.Get("token");

                        if (!string.IsNullOrEmpty(token))
                        {
                            try
                            {
                                var remoteLogin = RemoteAuthorizeHelper.ParseToken(token);
                                if (remoteLogin != null)
                                {
                                    var currentUser = await _User.GetName();

                                    if (remoteLogin.User != currentUser)
                                    {
                                        _logger.LogTrace("Удаленный пользователь {remote} не соответствует текущему {current}", remoteLogin.User, currentUser);
                                        if (!string.IsNullOrEmpty(currentUser))
                                        {
                                            _logger.LogTrace("Выход приложения");
                                            await LogoutUser();
                                            await Task.Delay(1000);
                                        }
                                        _logger.LogTrace("Авторизация удаленного пользователя");
                                        await AuthorizeRemoteUser(remoteLogin);
                                    }
                                }
                            }
                            catch (Exception eParse)
                            {
                                _logger.LogTrace("Ошибка разбора токена для авторизации {value}", eParse.Message);
                            }
                        }

                        var newSystemId = queryList.Get("systemId");

                        if (!string.IsNullOrEmpty(newSystemId))
                        {
                            _logger.LogTrace("Установка новой подсистемы {system}", newSystemId);
                            int.TryParse(newSystemId, out var systemID);
                            if (systemID >= SubsystemType.SUBSYST_ASO && systemID <= SubsystemType.SUBSYST_P16x && CheckSubsystemId(systemID))
                            {
                                Http.DefaultRequestHeaders.AddHeader(CookieName.SubsystemID, systemID.ToString());
                                var viewstatnotify = queryList.Get("viewstatnotify");
                                if (!string.IsNullOrEmpty(viewstatnotify))
                                {
                                    navigateTo = $"/{systemID}/Index/viewstatnotify";
                                }
                            }
                            else
                            {
                                _logger.LogTrace("Подсистема запрещена {system}", systemID);
                            }
                        }
                        _logger.LogTrace("Перенаправление по новому адресу {value}", navigateTo);

                        MyNavigationManager.NavigateTo(navigateTo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка разбора url адреса {message}", ex.Message);
            }
        }

        void CheckSubSystemId()
        {
            if (!CheckSubsystemId(SystemId))
            {
                _logger.LogTrace("Подсистема {id} запрещена", SystemId);
                MyNavigationManager.NavigateTo($"/", true, true);
            }
        }

        private async Task GetTimeLoadConnections()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetParams", new StringValue() { Value = nameof(ParamsAuthorize.TimeLoadConnections) });
            if (result.IsSuccessStatusCode)
            {
                var g = await result.Content.ReadFromJsonAsync<StringValue>() ?? new();
                uint.TryParse(g.Value, out var response);
                if (response == 0)
                {
                    response = 10;
                }
                MinTimeoutLoad = response;
            }
        }

        bool CheckSubsystemId(int systemId)
        {
            int response = systemId;
            if (!ConfStart.ASO && response == SubsystemType.SUBSYST_ASO)
            {
                response = ConfStart.UUZS ? SubsystemType.SUBSYST_SZS : ConfStart.STAFF ? SubsystemType.SUBSYST_GSO_STAFF : 0;
            }
            else if (!ConfStart.UUZS && response == SubsystemType.SUBSYST_SZS)
            {
                response = ConfStart.ASO ? SubsystemType.SUBSYST_ASO : ConfStart.STAFF ? SubsystemType.SUBSYST_GSO_STAFF : 0;
            }
            else if (!ConfStart.STAFF && response == SubsystemType.SUBSYST_GSO_STAFF)
            {
                response = ConfStart.ASO ? SubsystemType.SUBSYST_ASO : ConfStart.UUZS ? SubsystemType.SUBSYST_SZS : 0;
            }
            else if (!ConfStart.P16x && response == SubsystemType.SUBSYST_P16x)
            {
                response = ConfStart.ASO ? SubsystemType.SUBSYST_ASO : ConfStart.UUZS ? SubsystemType.SUBSYST_SZS : ConfStart.STAFF ? SubsystemType.SUBSYST_GSO_STAFF : 0;
            }

            return systemId == response;
        }


        public async ValueTask DisposeAsync()
        {
            MyNavigationManager.LocationChanged -= MyNavigationManager_LocationChanged;
            timer.Stop();
            timer.Dispose();

            if (timerLoadAccess != null)
            {
                timerLoadAccess.Stop();
                timerLoadAccess.Dispose();
            }
            await DisposeAuthHubAsync();
        }
    }
}

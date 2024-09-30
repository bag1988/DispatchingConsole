using System.Globalization;
using System.Net.Http.Json;
using BlazorLibrary.Helpers;
using BlazorLibrary.ServiceColection;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary.Interfaces;
using SensorM.GsoCore.SharedLibrary.PuSubModel;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared
{
    partial class Main : IAuthorizeHub
    {
        HubConnection? _authHubContext;

        StatusAuthorize? StateAuthorize { get; set; }

        bool IsProcessingAuthorize = false;

        ConflictGettingAccess? ConflictMessage;

        readonly System.Timers.Timer timerLoadAccess = new(TimeSpan.FromSeconds(1));

        uint LoadSecond = MinTimeoutLoad;

        int NumberAttempsErrorConnect = MaxNumberAttempsErrorConnect;
        const int MaxNumberAttempsErrorConnect = 10;
        async Task InitAuthHubContext()
        {
            try
            {
                timerLoadAccess.Elapsed += TimerLoadAccess_Elapsed;
                await CorrectPortConnect();
                try
                {
                    await StartConnectHub();
                }
                catch (Exception e)
                {
                    _logger.LogError("Ошибка подключения к серверу авторизации, {message}", e.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка инициализации подключения к серверу авторизации, {message}", ex.Message);
                MessageView?.AddError("", GsoRep["ERROR_CONNECT_TO_AUTHORIZE_SERVER"]);
            }
        }

        async Task StartConnectHub()
        {
            try
            {
                if (_authHubContext != null)
                {
                    await _authHubContext.StartAsync();
                    if (NumberAttempsErrorConnect != MaxNumberAttempsErrorConnect)
                    {
                        StateHasChanged();
                    }
                    if (_authHubContext.State == HubConnectionState.Connected)
                    {
                        _logger.LogTrace("Приложение подключено к серверу авторизации {url}", $"https://{IpAddressUtilities.GetHost(MyNavigationManager.BaseUri)}:{AppPortsInfo.DEVICECONSOLEAPPPORT}");
                        await GetCurrentLanguage();
                        _ = CheckUpdateServiceWorker();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка подключения к серверу авторизации, {message}", ex.Message);
            }

            if (_authHubContext != null && _authHubContext.State != HubConnectionState.Connected && NumberAttempsErrorConnect > 0)
            {
                int delay = NumberAttempsErrorConnect switch
                {
                    > 7 => 1000,
                    > 5 => 2000,
                    > 2 => 3000,
                    _ => 5000
                };
                await Task.Delay(delay);
                NumberAttempsErrorConnect--;
                if (NumberAttempsErrorConnect == 0)
                {
                    StateHasChanged();
                }
                if (_authHubContext.State != HubConnectionState.Connected)
                {
                    _logger.LogTrace("Повторная попытка (№ {value}) поключения к серверу авторизации", MaxNumberAttempsErrorConnect - NumberAttempsErrorConnect);
                    StateHasChanged();
                    await CorrectPortConnect();
                    _ = StartConnectHub();
                }
            }
        }

        async Task CorrectPortConnect()
        {
            if (AppPortsInfo.DEVICECONSOLEAPPPORT == 0)
            {
                if (_authHubContext != null)
                {
                    _authHubContext.Reconnected -= _authHubContext_Reconnected;
                    _authHubContext.Reconnecting -= _authHubContext_Reconnecting;
                }
                AppPortsInfo = await GetAppPortInfo();
                var urlSignal = $"https://{IpAddressUtilities.GetHost(MyNavigationManager.BaseUri)}:{AppPortsInfo.DEVICECONSOLEAPPPORT}/AuthorizeHub?{nameof(CookieName.AppId)}={Http.DefaultRequestHeaders.GetHeader(nameof(CookieName.AppId))}";
                _authHubContext = new HubConnectionBuilder().WithUrl(urlSignal).WithAutomaticReconnect(new RRetryPolicy()).Build();
                if (_authHubContext != null)
                {
                    _authHubContext.Reconnected += _authHubContext_Reconnected;
                    _authHubContext.Reconnecting += _authHubContext_Reconnecting;
                    _authHubContext.SubscribeViaInterface(this, typeof(IAuthorizeHub));
                }
            }

        }

        public async Task ReplayConnect()
        {
            NumberAttempsErrorConnect = MaxNumberAttempsErrorConnect;
            await StartConnectHub();
        }

        private Task _authHubContext_Reconnecting(Exception? arg)
        {
            StateHasChanged();
            _logger.LogTrace("Переподключение к серверу авторизации");
            return Task.CompletedTask;
        }

        private async Task _authHubContext_Reconnected(string? arg)
        {
            StateHasChanged();
            _logger.LogTrace("Восстановлено подключение к серверу авторизации");
            await JSRuntime.InvokeVoidAsync("sendKeepAliveWorker");
            await CheckAuthenticationApp();
            NumberAttempsUpdate = MaxNumberAttempsUpdate;
            _ = CheckUpdateServiceWorker();
            await CheckUpdate();
        }

        private void TimerLoadAccess_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (LoadSecond > 0)
            {
                LoadSecond--;
                StateHasChanged();
            }
            else if (timerLoadAccess != null)
            {
                ConflictMessage = null;
                timerLoadAccess.Stop();
            }
        }

        async Task SendToAuthHub(string methodName, object[]? args = null)
        {
            try
            {
                if (_authHubContext != null)
                {
                    if (_authHubContext.State == HubConnectionState.Connected)
                    {
                        await _authHubContext.SendCoreAsync(methodName, args ?? Array.Empty<object>());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки данных на сервер авторизации {methodName}: {message}", methodName, ex.Message);
            }
        }

        async Task<TData?> InvokeToAuthHub<TData>(string methodName, object?[]? args = null)
        {
            try
            {
                if (_authHubContext != null)
                {
                    if (_authHubContext.State == HubConnectionState.Connected)
                    {
                        return await _authHubContext.InvokeCoreAsync<TData>(methodName, args ?? Array.Empty<object>());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки данных на сервер авторизации {methodName}: {message}", methodName, ex.Message);
            }
            return default;
        }
        #region подписки

        public async Task Fire_ChangeLanguage(string value)
        {
            try
            {
                if (CultureInfo.CurrentCulture.Name != value)
                {
                    _logger.LogTrace("Применение локализации {message}", value);

                    var newCulture = new CultureInfo(value);
                    CultureInfo.CurrentCulture = newCulture;
                    CultureInfo.CurrentUICulture = newCulture;

                    await JSRuntime.InvokeVoidAsync("setCultureGlobal", value);
                    Http.DefaultRequestHeaders.AcceptLanguage.Clear();
                    Http.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue(value));
                    MyNavigationManager.Refresh();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка применение локализации, {message}", ex.Message);
            }
        }


        async Task GetCurrentLanguage()
        {
            try
            {
                var result = await InvokeToAuthHub<string?>("GetParamChangeLanguage");

                if (!string.IsNullOrEmpty(result))
                {
                    await Fire_ChangeLanguage(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка проверки локализации, {message}", ex.Message);
            }
        }

        async Task ChangeLanguage(string value)
        {
            try
            {
                var result = await InvokeToAuthHub<bool>("SetDefaultLanguage", [value]);
                if (result)
                {
                    await Fire_ChangeLanguage(value);
                }
                else
                {
                    _logger.LogError("Изменения локализации не произошло");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка изменения локализации, {message}", ex.Message);
            }
        }

        public async Task Fire_RefreshToken(string value)
        {
            try
            {
                _logger.LogTrace("Установка обновленного токена");
                var b = await InvokeToAuthHub<bool>("RefreshToken", [value]);
                if (b)
                {
                    await SetTokenAsync(value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка обновления токена, {message}", ex.Message);
            }
        }

        public async Task Fire_ChangeParamAccess(string value)
        {
            try
            {
                _logger.LogTrace("Изменены параметры доступа для пользователя {user}", value);
                var currentUser = await _User.GetName();
                if (!string.IsNullOrEmpty(currentUser) && value == currentUser)
                {
                    isPageLoad = false;
                    StateHasChanged();
                    await SendRefreshToken(currentUser);
                    MessageView?.AddMessage("", GsoRep["W_UPDATE_PARAM_ACCESS"]);
                    isPageLoad = true;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка обновления токена, {message}", ex.Message);
            }
        }

        public async Task Fire_ChangeParamAuthorize(string value)
        {
            try
            {
                _logger.LogTrace("Изменены параметры авторизации");
                await GetTimeLoadConnections();
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка установки значения для ожидания подключения, {message}", ex.Message);
            }
        }

        public async Task Fire_Logout(string value)
        {
            StateAuthorize = null;
            _logger.LogTrace("Выход пользователя, {message}", value);
            await LogoutUser();
        }

        /// <summary>
        /// Удаление токена
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task Fire_RemoveToken(string value)
        {
            _logger.LogTrace("Удаление токена, {message}", value);
            await RemoveToken();
        }

        /// <summary>
        /// отправка статуса авторизации
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task Fire_SendStatusAuthorize(StatusAuthorize value)
        {
            StateAuthorize = value;
            if (value < StatusAuthorize.GET_PARAMS || value == StatusAuthorize.USER_AUTHORIZE || value == StatusAuthorize.ALREADY_START)
            {
                IsProcessingAuthorize = false;
                if (value == StatusAuthorize.USER_AUTHORIZE)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        if (IsProcessingAuthorize == false)
                        {
                            StateAuthorize = null;
                            StateHasChanged();
                        }
                    });
                }
            }
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// ответ на авторизацию
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task Fire_ResponseGetAccessLogin(ResponseGetAccessLogin value)
        {
            if (!value.ResponseAccess)
            {
                await Fire_SendStatusAuthorize(StatusAuthorize.ACCESS_DENIED);
                MessageView?.AddMessage("", System.String.Format(GsoRep["W_ACCESS_DENIED"], value.WhoAnswerIpAddress));
            }
            else
            {
                await Fire_SendStatusAuthorize(StatusAuthorize.ACCESS_ALLOWED);
            }
        }

        /// <summary>
        /// конфликт получения доступа
        /// </summary>
        /// <param name="value">ip с которого пытаются получить доступ</param>
        /// <returns></returns>
        public Task Fire_ConflictGettingAccess(ConflictGettingAccess value)
        {
            _logger.LogTrace("Конфликт подключений, запрос с IP {ip}", value.WhoRequestIpAddress);
            LoadSecond = MinTimeoutLoad;
            if (timerLoadAccess != null)
            {
                timerLoadAccess.Start();
            }
            ConflictMessage = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// авторизация всех приложений на ip адресе
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task Fire_SetSharedToken(SharedToken value)
        {
            try
            {
                _logger.LogTrace("Установка единого токена");
                isPageLoad = false;
                StateHasChanged();
                var b = await InvokeToAuthHub<bool>("SetSharedToken", [value]);
                _logger.LogTrace("Токен сохранен на сервере успешно {value}", b);
                if (b)
                {
                    _logger.LogTrace("Записываем токен в локальное хранилище");
                    await SetTokenAsync(value.Token);
                }
                isPageLoad = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка установки единого {message}", ex.Message);
            }
            StateAuthorize = null;
            StateHasChanged();
        }

        public async Task Fire_SetToken(string value)
        {
            try
            {
                _logger.LogTrace("Установка токена");
                await SetTokenAsync(value);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка установки токена {message}", ex.Message);
            }
        }

        public Task Fire_RemoveConflictGettingAccess()
        {
            _logger.LogTrace("Удаление окна конфликта авторизации");
            ConflictMessage = null;
            if (timerLoadAccess != null)
            {
                timerLoadAccess.Stop();
            }
            StateHasChanged();
            return Task.CompletedTask;
        }

        #endregion

        async Task SendAnsewerConflict(bool access)
        {
            try
            {
                if (ConflictMessage != null)
                {
                    await SendToAuthHub("SendAnswerConflict", [new AnswerConflictGettingAccess(ConflictMessage.KeyAnswer, access)]);
                    ConflictMessage = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки ответа на запрос авторизации {message}", ex.Message);
            }
        }

        async Task<bool> SetTokenAsync(string token)
        {
            bool response = false;
            try
            {
                var claims = JwtParser.ParseIEnumerableClaimsFromJwt(token);
                var userName = new AuthorizUser(claims).UserName;
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userName))
                {
                    await _localStorage.SetTokenAsync(token);
                    await _localStorage.SetLastUserName(userName);
                    ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(claims);
                    response = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка установки токена {message}", ex.Message);
            }
            StateAuthorize = null;
            return response;
        }


        public async Task LogoutUser()
        {
            try
            {
                await RemoveToken();
                await SendToAuthHub("Logout");
                StateAuthorize = null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка выхода пользователя {message}", ex.Message);
            }
        }


        public async Task SendRefreshToken(string userName)
        {
            try
            {
                //Обновляем токен
                _logger.LogTrace("Отправка запроса на обновления токена");
                var result = await InvokeToAuthHub<string>("RefreshUser", [userName]);
                _logger.LogTrace("Получен новый токен от запроса на обновления токена, токен пустой {status}", string.IsNullOrEmpty(result));
                if (!string.IsNullOrEmpty(result))
                {
                    var claims = JwtParser.ParseIEnumerableClaimsFromJwt(result);
                    var newUserName = new AuthorizUser(claims).UserName;

                    if (newUserName != userName)
                    {
                        _logger.LogTrace("Пользователь не соответствует текущему, выход пользователя");
                        await LogoutUser();
                    }
                    else
                    {
                        _logger.LogTrace("Установка нового токена");
                        await SetTokenAsync(result);
                    }
                }
                else
                {
                    _logger.LogTrace("Токен пустой, выход пользователя");
                    await LogoutUser();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при попытке обновить токен {message}", ex.Message);
            }
        }

        async Task RemoveToken()
        {
            try
            {
                ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
                await _localStorage.RemoveAllAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка удаления токена {message}", ex.Message);
            }
        }

        public async Task AuthorizeUser(RequestLogin request)
        {
            try
            {
                _logger.LogTrace("Запуск авторизации пользователя {login}", request.User);
                StateAuthorize = null;
                IsProcessingAuthorize = true;
                await SendToAuthHub("AuthorizeUser", [request]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка авторизации: {message}", ex.Message);
            }
        }

        public async Task StopAuthorize()
        {
            try
            {
                await SendToAuthHub("StopAuthorize");
                StateAuthorize = null;
                IsProcessingAuthorize = false;
                await RemoveToken();
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка остановки авторизации пользователя: {message}", ex.Message);
            }
        }

        public async Task AuthorizeRemoteUser(RequestLogin request)
        {
            try
            {
                _logger.LogTrace("Запуск автоматической авторизации пользователя {login}", request.User);
                StateAuthorize = null;
                IsProcessingAuthorize = true;
                await SendToAuthHub("AuthorizeViaQueryParam", [request]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка авторизации удаленного пользователя: {message}", ex.Message);
            }
        }

        private async Task<AppPorts> GetAppPortInfo()
        {
            try
            {
                var result = await Http.PostAsync("api/v1/allow/GetAppPortInfo", JsonContent.Create(true));
                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadFromJsonAsync<AppPorts>() ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return new();
        }

        public async ValueTask DisposeAuthHubAsync()
        {
            if (_authHubContext != null)
            {
                _authHubContext.Reconnected -= _authHubContext_Reconnected;
                _authHubContext.Reconnecting -= _authHubContext_Reconnecting;
                await _authHubContext.DisposeAsync();
            }
        }
    }
}

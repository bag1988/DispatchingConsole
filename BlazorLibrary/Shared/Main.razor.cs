
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Timers;
using BlazorLibrary.Helpers;
using BlazorLibrary.Shared.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SharedLibrary;
using System.ComponentModel;
using SharedLibrary.Interfaces;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Routing;

namespace BlazorLibrary.Shared
{
    partial class Main : IAsyncDisposable, IPubSubMethod
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

        int SystemId => ParseUrlSegments.GetSystemId(MyNavigationManager.Uri);

        public static MessageViewList? MessageView = default!;

        bool isPageLoad = false;

        bool IsLoadRemoteAuth = false;

        protected override async Task OnInitializedAsync()
        {
            ConfStart = await GetConfStart();

            CheckSubSystemId();

            await CheckQuery();

            IsLoadRemoteAuth = false;
            StateHasChanged();

            await CheckUser();

            MyNavigationManager.LocationChanged += MyNavigationManager_LocationChanged;

            await JSRuntime.InvokeVoidAsync("HotKeys.ListenWindowKey");

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_RestartUi(string? value)
        {
            _logger.LogTrace("Restart Ui");
            if (isPageLoad)
            {
                MyNavigationManager.NavigateTo($"/{SystemId}/", true, true);
            }
            _HubContext.SetFuncForReconnect(null);
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_AllUserLogout(string? str)
        {
            _logger.LogTrace("All users logout");
            if (!string.IsNullOrEmpty(str))
            {
                MessageView?.AddError("", str);
            }
            await AuthenticationService.Logout();

            _HubContext.SetFuncForReconnect(Fire_RestartUi);
        }

        private void MyNavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
        {
            CheckSubSystemId();
        }

        private async Task CheckUser()
        {
            try
            {
                var s = await _localStorage.GetTokenAsync();
                bool isCheckUser = false;
                if (!string.IsNullOrEmpty(s))
                {
                    if (Http.DefaultRequestHeaders.Authorization == null)
                        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(MetaDataName.Bearer, s);

                    var result = await Http.PostAsJsonAsync("api/v1/allow/CheckUser", s);
                    if (result.IsSuccessStatusCode)
                    {
                        isCheckUser = await result.Content.ReadFromJsonAsync<bool>();
                    }
                }

                if (!isCheckUser)
                {
                    await AuthenticationService.Logout();
                }
                else
                {
                    _ = RefreshToken();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            isPageLoad = true;
        }

        async Task<string?> IsNeedRefreshToken()
        {
            var user = await AuthenticationService.GetUser();
            if (user != null && !string.IsNullOrEmpty(user.Identity?.Name))
            {
                if (await CheckOldActive())
                {
                    var exp = user.FindFirst(c => c.Type.Equals("exp"))?.Value;
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));

                    var now = DateTimeOffset.UtcNow;
                    if (expTime.AddMinutes(-10).CompareTo(now) < 0)
                        return user.Identity?.Name;
                }
            }
            return null;
        }

        async Task RefreshToken()
        {
            try
            {
                string userName = "";
                userName = await IsNeedRefreshToken() ?? "";
                if (!string.IsNullOrEmpty(userName))
                {
                    string urlRefresh = "api/v1/allow/RefreshUser";

                    //Обновляем токен
                    var result = await Http.PostAsJsonAsync(urlRefresh, userName);
                    if (result.IsSuccessStatusCode)
                    {
                        var token = await result.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(token))
                        {
                            var claims = JwtParser.ParseIEnumerableClaimsFromJwt(token);
                            var newUserName = new AuthorizUser(claims).UserName;

                            if (newUserName != userName)
                            {
                                await AuthenticationService.Logout();
                            }
                            else
                            {
                                await AuthenticationService.SetTokenAsync(token);
                            }
                        }
                        else
                        {
                            await AuthenticationService.Logout();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            await Task.Delay(TimeSpan.FromSeconds(30));

            _ = RefreshToken();
        }

        async Task<bool> CheckOldActive()
        {
            var dateTime = await _localStorage.GetLastActiveDateAsync();
            if (dateTime == null || dateTime?.AddHours(1).CompareTo(DateTime.Now) < 0)
            {
                await AuthenticationService.Logout();
                return false;
            }

            return true;
        }

        void OnSetActive()
        {
            _ = _localStorage.SetLastActiveDateAsync(DateTime.Now);
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
                if (MyNavigationManager.Uri.Contains("token=") || MyNavigationManager.Uri.Contains("systemId=") || MyNavigationManager.Uri.Contains("user="))
                {
                    var _url = new Uri(MyNavigationManager.Uri);

                    var queryList = HttpUtility.ParseQueryString(_url.Query);

                    if (queryList?.Count > 0)
                    {
                        IsLoadRemoteAuth = true;

                        var newSystemId = queryList.Get("systemId");
                        if (!string.IsNullOrEmpty(newSystemId))
                        {
                            int.TryParse(newSystemId, out int SystemID);
                            if (SystemID >= SubsystemType.SUBSYST_ASO && SystemID <= SubsystemType.SUBSYST_P16x)
                            {
                                var viewstatnotify = queryList.Get("viewstatnotify");
                                if (!string.IsNullOrEmpty(viewstatnotify))
                                {
                                    Http.DefaultRequestHeaders.AddHeader(CookieName.SubsystemID, SystemID.ToString());
                                    MyNavigationManager.NavigateTo($"/{SystemID}/Index/viewstatnotify");
                                }
                            }
                        }
                        else
                        {
                            MyNavigationManager.NavigateTo(ParseUrlSegments.AbsolutePath(MyNavigationManager.Uri));
                        }


                        var newToken = queryList.Get("token");
                        if (!string.IsNullOrEmpty(newToken))
                        {
                            if (await _User.GetUserId() > 0)
                            {
                                await AuthenticationService.Logout();
                            }
                            queryList.Remove("token");

                            await AuthenticationService.SetTokenAsync(newToken);
                        }
                        else
                        {
                            var newUser = queryList.Get("user");

                            if (!string.IsNullOrEmpty(newUser))
                            {
                                if (await _User.GetUserId() > 0)
                                {
                                    await AuthenticationService.Logout();
                                }
                                queryList.Remove("user");

                                var newPassword = queryList.Get("password");

                                if (!string.IsNullOrEmpty(newUser))
                                {
                                    queryList.Remove("password");

                                    await AuthenticationService.RemoteLogin(new() { User = newUser, Password = newPassword });
                                }
                            }
                        }
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
            int response = SystemId;
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

            if (SystemId != response)
            {
                _logger.LogTrace("Подсистема {id} запрещена", SystemId);
                MyNavigationManager.NavigateTo($"/{response}/", true, true);
            }
        }

        public ValueTask DisposeAsync()
        {
            MyNavigationManager.LocationChanged -= MyNavigationManager_LocationChanged;
            return _HubContext.DisposeAsync();
        }
    }
}

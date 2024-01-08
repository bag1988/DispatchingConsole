
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
using SharedLibrary.Extensions;
using System.ComponentModel;
using SharedLibrary.Interfaces;
using System.Threading;
using System.Web;
using SMDataServiceProto.V1;
using SCSChLService.Protocol.Grpc.Proto.V2;

namespace BlazorLibrary.Shared
{
    partial class Main : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public RenderFragment<int>? ChildContent { get; set; }

        [Parameter]
        public RenderFragment<int>? Menu { get; set; }

        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public int? Width { get; set; } = 250;

        public ConfigStart ConfStart = new();

        public int SubsystemID { get; set; } = 0;

        public static MessageViewList? MessageView = default!;

        ElementReference? main;

        bool isPageLoad = false;

        bool IsLoadRemoteAuth = false;

        protected override async Task OnInitializedAsync()
        {
            await Task.Yield();
            ConfStart = await GetConfStart();

            await CheckQuery(MyNavigationManager.Uri);

            await CheckUser();

            var subId = await _User.GetSubSystemID();
            await ChangeSubSystem(subId);

            await JSRuntime.InvokeVoidAsync("HotKeys.ListenWindowKey");

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_RestartUi(string? value)
        {
            Console.WriteLine("Restart Ui");
            if (isPageLoad)
            {
                MyNavigationManager.NavigateTo("/", true, true);
            }
            _HubContext.SetFuncForReconnect(null);
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_AllUserLogout(string? str)
        {
            Console.WriteLine("All users logout");
            if (!string.IsNullOrEmpty(str))
            {
                MessageView?.AddError("", str);
            }
            await AuthenticationService.Logout();

            _HubContext.SetFuncForReconnect(Fire_RestartUi);
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
                Console.WriteLine(ex.Message);
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
                Console.WriteLine(ex.Message);
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
                Console.WriteLine(ex.Message);
            }
            return new();
        }

        public async Task CheckQuery(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                try
                {
                    var _url = new Uri(uri);

                    var queryList = HttpUtility.ParseQueryString(_url.Query);

                    if (queryList?.Count > 0)
                    {
                        var newSystemId = queryList.Get("systemId");
                        if (!string.IsNullOrEmpty(newSystemId))
                        {
                            queryList.Remove("systemId");
                            int.TryParse(newSystemId, out int SystemID);
                            if (SystemID > 0 && SystemID <= 4)
                            {
                                await ChangeSubSystem(SystemID);
                            }
                        }

                        var newToken = queryList.Get("token");

                        if (!string.IsNullOrEmpty(newToken))
                        {
                            queryList.Remove("token");

                            await AuthenticationService.SetTokenAsync(newToken);
                        }

                        var newUser = queryList.Get("user");

                        if (!string.IsNullOrEmpty(newUser))
                        {
                            queryList.Remove("user");

                            var newPassword = queryList.Get("password");

                            if (!string.IsNullOrEmpty(newUser))
                            {
                                queryList.Remove("password");
                                IsLoadRemoteAuth = true;
                                await AuthenticationService.RemoteLogin(new() { User = newUser, Password = newPassword });
                            }
                        }

                        var newPath = _url.AbsolutePath;
                        if (queryList.Count > 0)
                            newPath = $"{newPath}?{queryList}";
                        MyNavigationManager.NavigateTo(newPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                IsLoadRemoteAuth = false;
            }
        }

        public async Task<int> ChangeSubSystem(int NewSubSystemID)
        {
            return await ChangeSubSytemId(NewSubSystemID);
        }

        private async Task<int> ChangeSubSytemId(int subsystemid)
        {
            if (SubsystemID != subsystemid)
            {
                var id = CheckSubSystemId(subsystemid);
                var b = await _User.SetSubsystemId(id);
                if (!b)
                {
                    MessageView?.AddError("", StartUIRep["IDS_ERRORCAPTION"]);
                }
                else
                    SubsystemID = id;
                StateHasChanged();
            }
            return SubsystemID;
        }

        int CheckSubSystemId(int subsystemid)
        {
            int response = subsystemid;
            if (!ConfStart.ASO && subsystemid == SubsystemType.SUBSYST_ASO)
            {
                response = ConfStart.UUZS ? SubsystemType.SUBSYST_SZS : ConfStart.STAFF ? SubsystemType.SUBSYST_GSO_STAFF : 0;
            }
            else if (!ConfStart.UUZS && subsystemid == SubsystemType.SUBSYST_SZS)
            {
                response = ConfStart.ASO ? SubsystemType.SUBSYST_ASO : ConfStart.STAFF ? SubsystemType.SUBSYST_GSO_STAFF : 0;
            }
            else if (!ConfStart.STAFF && subsystemid == SubsystemType.SUBSYST_GSO_STAFF)
            {
                response = ConfStart.ASO ? SubsystemType.SUBSYST_ASO : ConfStart.UUZS ? SubsystemType.SUBSYST_SZS : 0;
            }
            else if (!ConfStart.P16x && subsystemid == SubsystemType.SUBSYST_P16x)
            {
                response = ConfStart.ASO ? SubsystemType.SUBSYST_ASO : ConfStart.UUZS ? SubsystemType.SUBSYST_SZS : ConfStart.STAFF ? SubsystemType.SUBSYST_GSO_STAFF : 0;
            }
            return response;
        }

        public void RefrechMe()
        {
            ChildContent = null;
            StateHasChanged();
        }


        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }
    }
}

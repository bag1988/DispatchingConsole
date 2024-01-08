using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using BlazorLibrary.Helpers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components.Authorization;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;

namespace BlazorLibrary.ServiceColection
{
    public class AuthenticationService : IAuthenticationService
    {
        readonly HttpClient _http;
        readonly AuthenticationStateProvider _authStateProvider;
        readonly LocalStorage _localStorage;
        static bool IsSendLogout = false;
        public AuthenticationService(HttpClient http, AuthenticationStateProvider authStateProvider, LocalStorage localStorage)
        {
            _http = http;
            _authStateProvider = authStateProvider;
            _localStorage = localStorage;
        }

        async Task<ErrorLoginUser> SendLogin(RequestLogin request, bool isRemote)
        {
            ErrorLoginUser response = ErrorLoginUser.NoConnect;

            var oldAppId = await _localStorage.GetAppIdAsync();

            var appId = Guid.NewGuid().ToString();
            await _localStorage.SetAppIdAsync(appId);
            _http.DefaultRequestHeaders.AddHeader(nameof(CookieName.AppId), appId);
            if (!string.IsNullOrEmpty(oldAppId))
            {
                _http.DefaultRequestHeaders.AddHeader(nameof(CookieName.OldAppId), oldAppId);
            }
            var result = await _http.PostAsJsonAsync($"api/v1/allow/{(isRemote ? "AuthorizeRemoteUser" : "AuthorizeUser")}", request);
            if (result.IsSuccessStatusCode)
            {
                var responseLogin = await result.Content.ReadFromJsonAsync<ResultLoginUser>();

                if (responseLogin != null)
                {
                    response = responseLogin.Error;

                    if (response == ErrorLoginUser.Ok)
                    {
                        if (!await SetTokenAsync(responseLogin.Token))
                        {
                            response = ErrorLoginUser.NoConnect;
                        }
                    }
                }
            }
            _http.DefaultRequestHeaders.Remove(nameof(CookieName.OldAppId));
            return response;
        }


        public async Task<ErrorLoginUser> Login(RequestLogin request)
        {
            return await SendLogin(request, false);
        }

        public async Task<ErrorLoginUser> RemoteLogin(RequestLogin request)
        {
            return await SendLogin(request, true);
        }

        public async Task<bool> SetTokenAsync(string token)
        {
            var claims = JwtParser.ParseIEnumerableClaimsFromJwt(token);

            var userName = new AuthorizUser(claims).UserName;
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userName))
            {
                await _localStorage.SetTokenAsync(token);
                await _localStorage.SetLastUserName(userName);
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(MetaDataName.Bearer, token);

                SetNewUser(claims);
                return true;
            }
            return false;
        }

        public void SetNewUser(IEnumerable<Claim> claims)
        {
            ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(claims);
        }

        public async Task<ClaimsPrincipal> GetUser()
        {
            return await ((AuthStateProvider)_authStateProvider).GetUser();
        }

        public async Task Logout()
        {
            if (!IsSendLogout)
            {
                IsSendLogout = true;

                ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
                await _localStorage.RemoveAllAsync();
                _ = _http.PostAsync("api/v1/allow/Logout", null);
                _http.DefaultRequestHeaders.Authorization = null;

                IsSendLogout = false;
            }
        }
    }
}

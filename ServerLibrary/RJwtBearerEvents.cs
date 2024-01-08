using Google.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SensorM.GsoCommon.ServerLibrary.Utilities;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;

namespace ServerLibrary
{
    public class RJwtBearerEvents : IJwtBearerEvents<HttpRequest>
    {
        public void OnMessageReceived(HttpRequest message)
        {
            try
            {
                var httpRequest = message;
                var key = httpRequest.HttpContext.GetTokenName();

                var _tokens = httpRequest.HttpContext.RequestServices.GetService<UsersToken>();

                var header = httpRequest.Headers.Authorization.FirstOrDefault();

                if (_tokens != null && string.IsNullOrEmpty(header) && _tokens.GetTokenForIp(key) != null)
                {
                    UserCookie? userCookie = _tokens.GetTokenForIp(key);
                    if (!string.IsNullOrEmpty(userCookie?.UserToken))
                        httpRequest.Headers.Authorization = $"{MetaDataName.Bearer} {userCookie.UserToken}";
                }
            }
            catch { }            
        }
    }
}

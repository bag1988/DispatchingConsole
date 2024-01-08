using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Api;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Utilities;

namespace ServerLibrary.Extensions
{
    public static class HttpContextExtensions
    {
        static string CreateIp(this HttpContext context)
        {
            var r = new Regex("(\\d{1,3}).(\\d{1,3}).(\\d{1,3}).(\\d{1,3})");

            var localIp = r.Match(context.Connection.LocalIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

            var remoteIp = r.Match(context.Connection.RemoteIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

            int port = context.Connection.RemotePort;

            string? ip = remoteIp.FirstOrDefault();

            if (!remoteIp.Any() || remoteIp.First() == localIp.FirstOrDefault() || remoteIp.Skip(1).Take(3).SequenceEqual(localIp.Skip(1).Take(3)))
            {
                ip = "127.0.0.1";
                port = context.Connection.LocalPort;
            }
            return $"{ip}:{port}";
        }

        public static string GetOldTokenName(this HttpContext context)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{context.GetOldAppId()}"));
        }

        public static string GetTokenName(this HttpContext context)
        {            
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{context.GetAppId()}"));
        }

        public static string GetTokenNameForRemote(this HttpContext context)
        {           
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"remote-{context.GetAppId()}"));
        }

        static string GetAppId(this HttpContext context)
        {
            context.Request.Headers.TryGetValue(nameof(CookieName.AppId), out var result);
            var appId = result.FirstOrDefault();

            if (string.IsNullOrEmpty(appId))
            {
                appId = context.Request.Query.FirstOrDefault(x => x.Key == nameof(CookieName.AppId)).Value;
            }
            if (string.IsNullOrEmpty(appId))
            {
                appId = "none";
            }
            return appId;
        }

        static string GetOldAppId(this HttpContext context)
        {
            context.Request.Headers.TryGetValue(nameof(CookieName.OldAppId), out var result);
            var appId = result.FirstOrDefault();

            if (string.IsNullOrEmpty(appId))
            {
                appId = context.Request.Query.FirstOrDefault(x => x.Key == nameof(CookieName.OldAppId)).Value;
            }
            if (string.IsNullOrEmpty(appId))
            {
                appId = "none";
            }
            return appId;
        }
    }
}

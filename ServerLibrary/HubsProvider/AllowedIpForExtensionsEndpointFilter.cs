using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace SensorM.GsoCommon.ServerLibrary.HubsProvider
{

    public class AllowedIpForExtensionsEndpointFilter : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            try
            {
                var _checkingIpResolution = context.HttpContext.RequestServices.GetService<CheckingIpResolution>();

                if (_checkingIpResolution != null)
                {
                    var _logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger<AllowedIpForExtensionsEndpointFilter>();
                    var localIp = context.HttpContext?.Connection.LocalIpAddress?.MapToIPv4().ToString();
                    var submask = string.Join(".", (localIp ?? "127.0.0.1").Split(".").Take(3));
                    var allowedListIp = _checkingIpResolution.GetAllowedIpForNotifyEx().Result;

                    if (allowedListIp.Length > 0)
                    {
                        var urlConnect = context.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4().ToString();

                        StringValues result = new();

                        context.HttpContext?.Request.Headers.TryGetValue("Origin", out result);

                        if ((result.FirstOrDefault()?.Contains("chrome-extension://") ?? false) && !string.IsNullOrEmpty(urlConnect))
                        {
                            if (allowedListIp.Contains(urlConnect) || (urlConnect.StartsWith(submask) && (allowedListIp.Contains("127.0.0.1") || allowedListIp.Contains("0.0.0.0"))))
                            {
                                _logger?.LogTrace("Новое подключение расширения к хабу от {forUrl}", urlConnect);
                                return next(context);
                            }
                        }
                        _logger?.LogTrace("Подключение для {forUrl} запрещено, локальный хост {Host}", urlConnect, localIp);
                    }
                    else
                    {
                        _logger?.LogTrace("IP адреса не настроены, локальный хост {Host}", localIp);
                    }
                }
            }
            finally
            {

            }
            return new(Results.StatusCode((int)HttpStatusCode.Forbidden));
        }
    }
}

using Dapr.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SensorM.GsoCommon.ServerLibrary.Extensions;
using ServerLibrary.Extensions;
using SharedLibrary;
using Asp.Versioning;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [AllowAnonymous]
    public class PushController : Controller
    {
        private readonly ILogger<PushController> _logger;

        private readonly DaprClient _daprClient;

        public PushController(ILogger<PushController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateVAPIDKeys()
        //{
        //    using var activity = this.ActivitySourceForController()?.StartActivity();
        //    try
        //    {
        //        var list = await GetVapidDetails();

        //        var remoteIp = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                
        //        VapidDetails item = new();

        //        if (!list.Any(x => x.Subject == remoteIp))
        //        {
        //            item = VapidHelper.GenerateVapidKeys();
        //            item.Subject = remoteIp;
        //            list.Add(item);
        //            await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.VapidDetails, list);
        //        }
        //        else
        //        {
        //            item = list.First(x => x.Subject == remoteIp);
        //        }
        //        return Ok(item.PublicKey);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
        //        return BadRequest();
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SaveSettingSubscription(NotificationSubscription request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await GetSubscription();

                var agent = ParseUserAgent.GetBrowserNameWithVersion(Request.Headers.UserAgent.FirstOrDefault());

                var remoteIp = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
                request.Host = Request.Host.Value;
                request.IpClient = remoteIp;
                request.UserAgent = agent;

                if (!response.Any(x => x.Endpoint == request.Endpoint))
                {
                    response.Add(request);
                    await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.PushSetting, response);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
        }

        //async Task RemoveVapidKeyForRemoteIp(string? remoteIp)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(remoteIp))
        //        {
        //            var list = await GetVapidDetails();
        //            list?.RemoveAll(x => x.Subject == remoteIp);
        //            await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.VapidDetails, list);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
        //    }
        //}

        async Task RemoveSubscriptionForRemoteIp()
        {
            try
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

                if (!string.IsNullOrEmpty(remoteIp))
                {
                    var host = Request.Host.Value;
                    var agent = ParseUserAgent.GetBrowserNameWithVersion(Request.Headers.UserAgent.FirstOrDefault());

                    List<NotificationSubscription> response = await GetSubscription();
                    if (response.RemoveAll(x => x.IpClient == remoteIp && x.Host == host && x.UserAgent == agent) > 0)
                        await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.PushSetting, response);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSubscriptionSetting()
        {
            try
            {
                using var activity = this.ActivitySourceForController()?.StartActivity();
                await RemoveSubscriptionForRemoteIp();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckSubscriptionSetting(NotificationSubscription request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            bool b = false;
            if (!string.IsNullOrEmpty(request.Endpoint))
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

                if (!string.IsNullOrEmpty(remoteIp))
                {
                    //var response = await GetSubscription();
                    //var list = await GetVapidDetails();

                    //if (response.Any(x => x.Endpoint == request.Endpoint) && list.Any(x => x.Subject == remoteIp))
                    //{
                    //    b = true;
                    //}
                    //else
                    //{
                    //    if (list.Any(x => x.Subject == remoteIp))
                    //    {
                    //        await SaveSettingSubscription(request);
                    //        b = true;
                    //    }
                    //    else
                    //    {
                    //        await RemoveSubscriptionForRemoteIp();
                    //    }
                    //}
                }
            }
            return Ok(b);
        }


        /// <summary>
        /// Получаем настройки push уведомления
        /// </summary>
        /// <returns></returns>
        private async Task<List<NotificationSubscription>> GetSubscription()
        {
            List<NotificationSubscription> response = new();
            try
            {
                response = await _daprClient.GetStateAsync<List<NotificationSubscription>?>(StateNameConst.StateStore, StateNameConst.PushSetting) ?? new();
            }
            catch (Exception ex)
            {
                try
                {
                    await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.PushSetting, new List<NotificationSubscription>());
                }
                catch (Exception e)
                {
                    _logger.WriteLogError(e, Request.RouteValues["action"]?.ToString());
                }
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }

            return response;
        }


        //private async Task<List<VapidDetails>> GetVapidDetails()
        //{
        //    List<VapidDetails> response = new();
        //    try
        //    {
        //        response = await _daprClient.GetStateAsync<List<VapidDetails>?>(StateNameConst.StateStore, StateNameConst.VapidDetails) ?? new();
        //    }
        //    catch (Exception ex)
        //    {
        //        try
        //        {
        //            await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.VapidDetails, new List<VapidDetails>());
        //        }
        //        catch (Exception e)
        //        {
        //            _logger.WriteLogError(e, Request.RouteValues["action"]?.ToString());
        //        }
        //        _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
        //    }

        //    return response;
        //}
    }
}

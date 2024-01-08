using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using ServerLibrary.HubsProvider;
using SharedLibrary;
using SharedLibrary.Interfaces;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [Route("/[action]")]
    [AllowAnonymous]
    public class PubSubController : Controller, IPubSubMethod
    {
        private readonly ILogger<PubSubController> _logger;

        private readonly IHubContext<SharedHub> _hubContext;

        public PubSubController(ILogger<PubSubController> logger, IHubContext<SharedHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        private async Task SetCookie<TData>(string NameNotify, TData ValueNotify)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(NameNotify, ValueNotify);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        #region Dapr Subscribe

        [HttpPost]
        public async Task Fire_RestartUi(string? value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_RestartUi), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateSituation(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await _hubContext.Clients.All.SendAsync(nameof(DaprMessage.Fire_UpdateSituation), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateTermDevice(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateTermDevice), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_InsertDeleteTermDevice(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteTermDevice), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateTermDevicesGroup(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateTermDevicesGroup), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_InsertDeleteTermDevicesGroup(long value)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteTermDevicesGroup), value);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_AllUserLogout([FromBody] string request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_AllUserLogout), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateMessage([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateMessage), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateList(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateList), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateAbonent(byte[] AbonentItemByte)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateAbonent), AbonentItemByte);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_UpdateControllingDevice([FromBody] long request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_UpdateControllingDevice), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_InsertDeleteSituation(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteSituation), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_InsertDeleteMessage([FromBody] ulong request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteMessage), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_InsertDeleteList(byte[] request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteList), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_InsertDeleteAbonent([FromBody] byte[] AbonentItemByte)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteAbonent), AbonentItemByte);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }

        [HttpPost]
        public async Task Fire_InsertDeleteControllingDevice([FromBody] long request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                await SetCookie(nameof(DaprMessage.Fire_InsertDeleteControllingDevice), request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
        }
        #endregion
    }
}

using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
//using Dapr.Client;
using SMDataServiceProto.V1;
using static SMSSGsoProto.V1.SMSSGso;
using System.Text.RegularExpressions;
using static SMDataServiceProto.V1.SMDataService;
using SensorM.GsoCommon.ServerLibrary.Utilities;
using SensorM.GsoCore.SharedLibrary;
using Asp.Versioning;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/allow/[action]")]
    [AllowAnonymous]
    public class AuthorizeController : Controller
    {
        private readonly SMSSGsoClient _SMGso;
        private readonly AuthorizedInfo _userInfo;
        private readonly SMDataServiceClient _SMData;
        private readonly ILogger<AuthorizeController> _logger;
        private readonly WriteLog _Log;
        private readonly IConfiguration _conf;
        private readonly UsersToken _tokens;

        public AuthorizeController(ILogger<AuthorizeController> logger, SMSSGsoClient data, WriteLog log, AuthorizedInfo userInfo, IConfiguration conf, SMDataServiceClient sMData, UsersToken tokens)
        {
            _logger = logger;
            _SMGso = data;
            _Log = log;
            _userInfo = userInfo;
            _conf = conf;
            _SMData = sMData;
            _tokens = tokens;
        }

        async Task<IActionResult> BeginUserSess(RequestLogin request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            activity?.AddTag("Пользователь", request.User);
            activity?.AddTag("Удаленный IP", HttpContext.Connection.RemoteIpAddress?.ToString());

            AuthResponse? user = null;
            try
            {
                _tokens.RemoveTokenForIp(HttpContext.GetOldTokenName());

                ResultLoginUser response = new()
                {
                    UserName = request.User,
                    Error = ErrorLoginUser.NoAccess
                };

                var r = new Regex("(\\d{1,3}).(\\d{1,3}).(\\d{1,3}).(\\d{1,3})");

                var localIp = r.Match(HttpContext.Connection.LocalIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

                var remoteIp = r.Match(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

                var listIP = await _SMData.GetServerIPAddressesAsync(new Google.Protobuf.WellKnownTypes.Empty());

                _logger.LogInformation(@"Вход по адресу {local}, с Ip-адреса {remote}, адреса в настройках {ipList}", HttpContext.Connection.LocalIpAddress?.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), listIP?.Array);

                bool isAccessBegin = false;
                if ((listIP?.Array.Select(x => IpAddressUtilities.GetHost(x)).Contains(remoteIp.FirstOrDefault()) ?? false) || remoteIp.FirstOrDefault() == localIp.FirstOrDefault() || remoteIp.Skip(1).Take(3).SequenceEqual(localIp.Skip(1).Take(3)))
                {
                    isAccessBegin = true;
                }
                else
                {
                    var checkAccess = await _SMGso.IsContenIpInArmWhiteListByParamsAsync(new SMDataServiceProto.V1.String() { Value = remoteIp.FirstOrDefault() });
                    isAccessBegin = checkAccess?.Value ?? false;

                    _logger.LogInformation(@"Доступ для Ip-адреса {remote} {Access}", remoteIp.FirstOrDefault(), (isAccessBegin ? "разрешен" : "запрещен"));
                }

                if (isAccessBegin)
                {
                    user = await _SMGso.BeginUserSessAsync(request, cancellationToken: HttpContext.RequestAborted);

                    await _Log.Write(Source: (int)GSOModules.Security_Module, EventCode: 129, Info: request.User, SubsystemID: 5);

                    if (user?.IsAuthSuccessful == true)
                    {
                        UserCookie newUser = new()
                        {
                            UserToken = user.Token,
                            RefreshToken = request.Password
                        };

                        _tokens.AddTokenForIp(HttpContext.GetTokenName(), newUser);

                        response.Token = user.Token;

                        response.Error = ErrorLoginUser.Ok;
                    }
                    else
                    {
                        response.Error = ErrorLoginUser.NoFindUser;
                    }
                }
                else
                {
                    response.Error = ErrorLoginUser.NoAccess;
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AuthorizeRemoteUser(RequestLogin request)
        {
            try
            {
                return await BeginUserSess(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> AuthorizeUser(RequestLogin request)
        {
            try
            {
                request.Password = AesEncrypt.EncryptString(request.Password);
                return await BeginUserSess(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshUser([FromBody] string NameUser)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            activity?.AddTag("Пользователь", NameUser);
            AuthResponse? user = null;
            try
            {
                UserCookie? userCookie = _tokens.GetTokenForIp(HttpContext.GetTokenName());
                if (userCookie != null)
                {
                    user = await _SMGso.BeginUserSessAsync(new RequestLogin()
                    {
                        User = NameUser,
                        Password = userCookie.RefreshToken ?? ""
                    }, cancellationToken: HttpContext.RequestAborted);

                    if (user?.IsAuthSuccessful == true)
                    {
                        userCookie.UserToken = user.Token;

                        _tokens.AddTokenForIp(HttpContext.GetTokenName(), userCookie);
                        return Ok(user.Token);
                    }
                }
                _tokens.RemoveTokenForIp(HttpContext.GetTokenName());
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckUser([FromBody] string token)
        {
            try
            {
                var IsAuth = JwtParser.IsValidToken(token);
                UserCookie? userCookie = _tokens.GetTokenForIp(HttpContext.GetTokenName());
                if (!IsAuth || userCookie == null || !token.Equals(userCookie.UserToken))
                {
                    Logout();
                    return Ok(false);
                }
                var statusSess = await _SMGso.GetUserSessStatusAsync(new Int32Value() { Value = _userInfo.GetInfo?.UserSessID ?? 0 });
                var IsLogoutAll = statusSess.Value == 1 ? true : false;

                return Ok(IsLogoutAll ? IsAuth : false);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            _tokens.RemoveTokenForIp(HttpContext.GetTokenName());

            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            return Ok();
        }

        [HttpPost]
        public IActionResult GetProductVersionNumberMajor()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_VERSION_NUMBER_MAJOR"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetProductVersionNumberMinor()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_VERSION_NUMBER_MINOR"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetProductVersionNumberPatch()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_VERSION_NUMBER_PATCH"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetBuildNumber()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_BUILD_NUMBER"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult PVersionFull()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var productVersion = new ProductVersion(
                CompanyName: _conf["COMPANY_NAME"],
                ProductName: _conf["PRODUCT_NAME"],
                ProductVersionNumberMajor: string.Empty,// _conf["PRODUCT_VERSION_NUMBER_MAJOR"],
                ProductVersionNumberMinor: string.Empty,// _conf["PRODUCT_VERSION_NUMBER_MINOR"],
                ProductVersionNumberPatch: string.Empty,// _conf["PRODUCT_VERSION_NUMBER_PATCH"],
                BuildNumber: AssemblyNames.GetVersionPKO ?? string.Empty);
                return Ok(productVersion);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetConfStart()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                ConfigStart response = new();
                _conf.GetSection("ConfStart").Bind(response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetLoggingSetting()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                Dictionary<string, string> conf = new Dictionary<string, string>();
                _conf.GetRequiredSection("Logging")?.GetRequiredSection("LogLevel").Bind(conf);
                return Ok(conf);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> WriteLog(WriteLog2Request request)
        {
            if (!await _Log.Write(request))
                return BadRequest();
            return Ok();
        }

    }
}

using Google.Protobuf.WellKnownTypes;
using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SensorM.GsoCommon.ServerLibrary;
using SensorM.GsoCommon.ServerLibrary.Utilities;
using SensorM.GsoCore.SharedLibrary.Interfaces;
using SensorM.GsoCore.SharedLibrary.PuSubModel;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using Microsoft.Extensions.Configuration;
using SensorM.GsoCommon.ServerLibrary.Models;

namespace ServerLibrary.HubsProvider
{
    public class AuthorizeHub : Hub<IAuthorizeHub>
    {
        private readonly ILogger<AuthorizeHub> _logger;
        private readonly SMDataServiceClient _SMData;
        private readonly SMSSGsoClient _SMGso;
        private readonly IUsersAuthToken _tokens;
        private readonly WriteLog _Log;
        private readonly CheckingIpResolution _checkingIpResolution;

        readonly static Dictionary<string, CancellationTokenSource> ProssecingList = new();

        readonly static Dictionary<Guid, bool> AnswerList = new();

        readonly static List<LanguageInfo> LanguageList = new();

        static bool IsOnStartAuthorize = false;
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly IConfiguration _conf;

        public AuthorizeHub(ILogger<AuthorizeHub> logger, IUsersAuthToken tokens, SMDataServiceClient sMData, SMSSGsoClient sMGso, CheckingIpResolution checkingIpResolution, WriteLog log, IConfiguration conf)
        {
            _logger = logger;
            _tokens = tokens;
            _SMData = sMData;
            _SMGso = sMGso;
            _checkingIpResolution = checkingIpResolution;
            _Log = log;
            _conf = conf;
        }

        private CancellationToken GetToken(string contextId)
        {
            if (ProssecingList.ContainsKey(contextId))
            {
                ProssecingList[contextId].Cancel();
                ProssecingList[contextId].Dispose();
                ProssecingList.Remove(contextId);
            }
            CancellationTokenSource token = new();
            ProssecingList.Add(contextId, token);
            return token.Token;
        }

        void RemoveTokenAuthorize(string contextId, bool dispose = true)
        {
            try
            {
                if (ProssecingList.ContainsKey(contextId))
                {
                    ProssecingList[contextId].Cancel();
                    if (dispose)
                    {
                        ProssecingList[contextId].Dispose();
                        ProssecingList.Remove(contextId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(RemoveTokenAuthorize));
            }
        }

        public void StopAuthorize()
        {
            try
            {
                RemoveTokenAuthorize(Context.ConnectionId, false);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(StopAuthorize));
            }
        }
        public void SendAnswerConflict(AnswerConflictGettingAccess request)
        {
            try
            {
                var key = request.KeyAnswer.ToString();
                if (AnswerList.ContainsKey(request.KeyAnswer))
                {
                    AnswerList[request.KeyAnswer] = request.Answer;
                }

                if (ProssecingList.ContainsKey(key))
                {
                    RemoveTokenAuthorize(key, false);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendAnswerConflict));
            }
        }

        async Task SendStatusAuthorize(StatusAuthorize status)
        {
            try
            {
                await Clients.Caller.Fire_SendStatusAuthorize(status);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendStatusAuthorize));
            }
        }

        async Task<bool> CallerIsPods(CancellationToken token = default)
        {
            try
            {
                var origin = Context.GetHttpContext()?.GetHeaderOrigin();
                var inAppPort = IpAddressUtilities.ParseUri(origin).Port;
                if (inAppPort > 0)
                {
                    _logger.LogTrace("Получение списка портов для HTTPS подключения");
                    var dispatchingconsole_app_https_port = (await _SMGso.GetAppPortsAsync(new BoolValue() { Value = true }, cancellationToken: token))?.DISPATCHINGCONSOLEAPPPORT;
                    _logger.LogTrace("Получение списка портов для HTTP подключения");
                    var dispatchingconsole_app_http_port = (await _SMGso.GetAppPortsAsync(new BoolValue() { Value = false }, cancellationToken: token))?.DISPATCHINGCONSOLEAPPPORT;
                    _logger.LogTrace("Порт подключения принадлежит ПОДС {value}", inAppPort == dispatchingconsole_app_https_port || inAppPort == dispatchingconsole_app_http_port);
                    return inAppPort == dispatchingconsole_app_https_port || inAppPort == dispatchingconsole_app_http_port;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка получения информации о портах: {message}", ex.Message);
            }
            return false;
        }


        public async Task<string?> GetParamChangeLanguage()
        {
            try
            {
                var paramLanguage = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.IndividualLocalization) });
                bool.TryParse(paramLanguage?.Value, out var result);
                if (result != true)
                {
                    var context = Context.GetHttpContext();
                    var urlConnect = context?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                    return LanguageList.FirstOrDefault(x => x.IpAddress == urlConnect)?.Language;
                }
                else
                {
                    LanguageList.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка получения параметра локализации {message}", ex.Message);
            }
            return null;
        }

        public async Task<bool> SetDefaultLanguage(string? value)
        {
            try
            {
                var paramLanguage = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.IndividualLocalization) });
                bool.TryParse(paramLanguage?.Value, out var result);
                if (!string.IsNullOrEmpty(value) && result != true)
                {
                    var context = Context.GetHttpContext();
                    var urlConnect = context?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                    LanguageList.ForEach(x =>
                    {
                        if (x.IpAddress == urlConnect)
                        {
                            x.Language = value;
                        }
                    });

                    var appId = context?.GetHeaderAppId() ?? "localhost";
                    if (!LanguageList.Any(x => x.AppId == appId))
                    {
                        LanguageList.Add(new LanguageInfo(urlConnect, "", appId, value));
                    }
                    await Clients.GroupExcept(urlConnect, Context.ConnectionId).Fire_ChangeLanguage(value);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки общего параметра локализации {message}", ex.Message);
            }
            return false;
        }

        async Task BeginUserSess(RequestLogin request, string ipConnect, string appId, CancellationToken tokenStopAuthorize)
        {
            try
            {
                RequestLoginEx requestLoginEx = new()
                {
                    Ip = ipConnect,
                    Password = request.Password,
                    User = request.User
                };

                _logger.LogTrace("Инициализация авторизации пользователя {login}", request.User);

                var isAccessBegin = await _checkingIpResolution.CheckWhiteList(tokenStopAuthorize);

                if (isAccessBegin.IsWhite && isAccessBegin.ArmWhiteType == SMSSGsoProto.V1.ArmWhiteType.ArmWhiteSupervisor && !(await CallerIsPods(tokenStopAuthorize)))
                {
                    isAccessBegin.IsWhite = false;
                }

                if (isAccessBegin.IsWhite)
                {
                    _logger.LogTrace("Получение параметров авторизации");
                    await SendStatusAuthorize(StatusAuthorize.GET_PARAMS);//"Получение параметров авторизации"

                    //единая авторизация
                    bool unifiedAuthorize = false;
                    // множество подключений
                    bool deniedMultipleConnections = false;
                    try
                    {
                        var paramUnifiedAuthorize = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.UnifiedAuthorize) }, cancellationToken: tokenStopAuthorize);

                        //разрешить множество подключений
                        var paramDeniedMultipleConnections = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.DeniedMultipleConnections) }, cancellationToken: tokenStopAuthorize);

                        bool.TryParse(paramUnifiedAuthorize?.Value, out unifiedAuthorize);
                        bool.TryParse(paramDeniedMultipleConnections?.Value, out deniedMultipleConnections);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Ошибка получение параметров авторизации: {message}", ex.Message);
                        await SendStatusAuthorize(StatusAuthorize.NO_CONNECT);
                        RemoveTokenAuthorize(Context.ConnectionId);
                        return;
                    }

                    _logger.LogTrace("Включена единая авторизация: {value}", unifiedAuthorize);
                    _logger.LogTrace("Разрешено множество подключений: {value}", deniedMultipleConnections);

                    var idAnswer = Guid.NewGuid();
                    AnswerList.Add(idAnswer, true);

                    //если запрещены множественные подключения
                    if (deniedMultipleConnections)
                    {
                        await SendStatusAuthorize(StatusAuthorize.CHECK_SESS_ID);//"Проверка активной сессии"

                        //Получаем активные IP адреса для логина (UserSessList)
                        var conflictContextId = await _tokens.GetOtherIpForUser(ipConnect, request.User);

                        _logger.LogTrace("Получен список активных ip адресов {count} для логина {login}", conflictContextId?.Count(), request.User);
                        if (conflictContextId != null && conflictContextId.Any())
                        {
                            var tokenAnswer = GetToken(idAnswer.ToString());
                            try
                            {
                                await SendStatusAuthorize(StatusAuthorize.START_WAIT);//Запуск ожидания ответа

                                await Clients.Clients(conflictContextId.Select(x => x.ContextId)).Fire_ConflictGettingAccess(new ConflictGettingAccess(ipConnect, idAnswer));
                                try
                                {
                                    if (!tokenStopAuthorize.IsCancellationRequested)
                                    {
                                        var paramTimeLoad = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.TimeLoadConnections) }, cancellationToken: Context.ConnectionAborted);
                                        uint.TryParse(paramTimeLoad?.Value, out var timeLoad);
                                        if (timeLoad == 0)
                                        {
                                            timeLoad = 10;
                                        }
                                        _logger.LogTrace("Установленое время ожидания {time}", timeLoad);
                                        await Task.Delay(TimeSpan.FromSeconds(timeLoad), CancellationTokenSource.CreateLinkedTokenSource([tokenAnswer, tokenStopAuthorize]).Token);
                                    }
                                }
                                catch
                                {
                                    _logger.LogTrace("Истекло время ожидания разрешения конфликта авторизации, запрос был прерван {cancel}", tokenAnswer.IsCancellationRequested);
                                    if (!tokenAnswer.IsCancellationRequested && !tokenStopAuthorize.IsCancellationRequested)
                                    {
                                        await SendStatusAuthorize(StatusAuthorize.NO_ANSWER);//нет ответа
                                    }
                                }

                                await Clients.Clients(conflictContextId.Select(x => x.ContextId)).Fire_RemoveConflictGettingAccess();

                                if (!tokenStopAuthorize.IsCancellationRequested)
                                {
                                    if (AnswerList[idAnswer])
                                    {
                                        await Clients.Clients(conflictContextId.Select(x => x.ContextId)).Fire_RemoveToken("конфликт ip адресов");

                                        await _tokens.RemoveTokenForUserName(request.User);
                                    }
                                    await Clients.Caller.Fire_ResponseGetAccessLogin(new ResponseGetAccessLogin(conflictContextId.First().IpAddress, AnswerList[idAnswer]));
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError("Ошибка разрешения конфликта авторизации, {message}", e.Message);
                            }
                            RemoveTokenAuthorize(idAnswer.ToString());
                        }
                        else
                        {
                            await SendStatusAuthorize(StatusAuthorize.NO_ACTIVE_SESS_ID);//"Активных сессий нет, запуск авторизации пользователя"
                        }
                    }


                    _logger.LogTrace("Проверка разрешено ли продолжение авторизации: {value}", AnswerList[idAnswer] && !tokenStopAuthorize.IsCancellationRequested);
                    //разрешено подключение
                    if (AnswerList[idAnswer] && !tokenStopAuthorize.IsCancellationRequested)
                    {
                        try
                        {
                            await SendStatusAuthorize(StatusAuthorize.BEGIN_USER);//проверка пользователя и авторизация в подсистеме
                                                                                  //авторизуем и записываем в сессию ip                        
                            var user = await _SMGso.BeginUserSessExAsync(requestLoginEx, cancellationToken: tokenStopAuthorize);

                            _logger.LogTrace("Авторизация прошла успешно: {value}", user?.IsAuthSuccessful == true);

                            if (!tokenStopAuthorize.IsCancellationRequested)
                            {
                                if (user?.IsAuthSuccessful == true)
                                {
                                    UserInfoConnect newUser = new(request.User, user.Token, request.Password, ipConnect, Context.ConnectionId);

                                    if (unifiedAuthorize)
                                    {
                                        await _tokens.RemoveTokenForIpAddress(ipConnect);
                                    }
                                    //сохраняем токен 
                                    await _tokens.AddTokenForAppId(appId, newUser);

                                    await Clients.Caller.Fire_SetToken(user.Token);

                                    //если единая авторизация отправляем всем приложениям команду на применение токена
                                    if (unifiedAuthorize && isAccessBegin.ArmWhiteType != SMSSGsoProto.V1.ArmWhiteType.ArmWhiteSupervisor)
                                    {
                                        await Clients.GroupExcept(ipConnect, Context.ConnectionId).Fire_SetSharedToken(new SharedToken(user.Token, request.Password));
                                    }
                                    await SendStatusAuthorize(StatusAuthorize.USER_AUTHORIZE);//Авторизация прошла успешно           
                                }
                                else
                                {
                                    await SendStatusAuthorize(StatusAuthorize.NO_FIND_USER);//Пользователь не найден                           
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError("Ошибка авторизации: {message}", ex.Message);
                        }                        
                    }
                    AnswerList.Remove(idAnswer);
                }
                else
                {
                    await SendStatusAuthorize(StatusAuthorize.NO_ACCESS);//Доступ запрещен
                }
            }
            catch (Exception e)
            {
                if (!tokenStopAuthorize.IsCancellationRequested)
                {
                    await SendStatusAuthorize(StatusAuthorize.ERROR);
                }
                _logger.WriteLogError(e, nameof(BeginUserSess));
            }
            RemoveTokenAuthorize(Context.ConnectionId);
        }

        /// <summary>
        /// Для запущенных, но не авторизованных приложениях
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>

        public async Task<bool> SetSharedToken(SharedToken token)
        {
            bool response = false;
            try
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(3));
                var httpContext = Context.GetHttpContext();
                //единая авторизация
                var paramUnifiedAuthorize = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.UnifiedAuthorize) }, cancellationToken: Context.ConnectionAborted);
                if (httpContext != null && bool.TryParse(paramUnifiedAuthorize?.Value, out var unifiedAuthorize) && unifiedAuthorize)
                {
                    var ipConnect = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                    var sharedUserToken = await _tokens.FindSharedTokenForIp(ipConnect, httpContext.GetHeaderAppId());
                    var currentUserInfo = await _tokens.GetTokenForAppId(httpContext.GetHeaderAppId());
                    var claims = JwtParser.ParseIEnumerableClaimsFromJwt(token.Token);
                    if (claims.Any(x => x.Type == nameof(AuthorizUser.UserSessID)) && ((sharedUserToken == null && currentUserInfo != null && !token.Token.Equals(currentUserInfo.UserToken)) || (sharedUserToken != null && token.Token.Equals(sharedUserToken.UserToken))))
                    {
                        var sessId = claims.First(x => x.Type == nameof(AuthorizUser.UserSessID));
                        var userName = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
                        if (int.TryParse(sessId.Value, out var userSessId) && !string.IsNullOrEmpty(userName))
                        {
                            var statusSess = _SMGso.GetUserSessStatus(new Int32Value() { Value = userSessId });
                            await _tokens.RemoveTokenForAppId(httpContext.GetHeaderAppId());
                            if (statusSess?.Value == 1)
                            {
                                UserInfoConnect newToken = new(userName, token.Token, token.RefreshToken, ipConnect, Context.ConnectionId);
                                //сохраняем токен 
                                await _tokens.AddTokenForAppId(httpContext.GetHeaderAppId(), newToken);
                                response = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetSharedToken));
            }
            SemaphoreRelease();
            return response;
        }

        public async Task AuthorizeViaQueryParam(RequestLogin request)
        {
            try
            {
                if (!IsOnStartAuthorize)
                {
                    IsOnStartAuthorize = true;
                    try
                    {
                        await semaphore.WaitAsync(TimeSpan.FromSeconds(3));
                        var httpContext = Context.GetHttpContext();
                        if (httpContext != null)
                        {
                            var token = GetToken(Context.ConnectionId);
                            await BeginUserSess(request, httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1", httpContext.GetHeaderAppId(), token);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Ошибка запуска авторизации пользователя, {message}", e.Message);
                    }
                    SemaphoreRelease();
                    IsOnStartAuthorize = false;
                }
                else
                {
                    await SendStatusAuthorize(StatusAuthorize.ALREADY_START);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(AuthorizeViaQueryParam));
            }
        }

        public async Task AuthorizeUser(RequestLogin request)
        {
            try
            {
                request.Password = AesEncrypt.EncryptString(request.Password);
                await AuthorizeViaQueryParam(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(AuthorizeViaQueryParam));
            }
        }

        public async Task<string> RefreshUser(string NameUser)
        {
            string response = string.Empty;
            try
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(3));
                var httpContext = Context.GetHttpContext();
                if (httpContext != null)
                {
                    AuthResponse? user = null;
                    var ipConnect = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                    _logger.LogTrace("Обновление токена для ip адреса {ip}", ipConnect);

                    var userCookie = await _tokens.GetTokenForAppId(httpContext.GetHeaderAppId());
                    if (userCookie != null)
                    {
                        var isAccessBegin = await _checkingIpResolution.CheckWhiteList();

                        if (isAccessBegin.IsWhite && isAccessBegin.ArmWhiteType == SMSSGsoProto.V1.ArmWhiteType.ArmWhiteSupervisor && !(await CallerIsPods(Context.ConnectionAborted)))
                        {
                            isAccessBegin.IsWhite = false;
                        }

                        if (isAccessBegin.IsWhite)
                        {
                            user = _SMGso.BeginUserSessEx(new RequestLoginEx()
                            {
                                User = NameUser,
                                Ip = ipConnect,
                                Password = userCookie.RefreshToken
                            }, cancellationToken: httpContext.RequestAborted);

                            if (user?.IsAuthSuccessful == true)
                            {
                                var updateToken = new UserInfoConnect(NameUser, user.Token, userCookie.RefreshToken, ipConnect, Context.ConnectionId);
                                await _tokens.AddTokenForAppId(httpContext.GetHeaderAppId(), updateToken);
                                if (isAccessBegin.ArmWhiteType != SMSSGsoProto.V1.ArmWhiteType.ArmWhiteSupervisor)
                                {
                                    //единая авторизация
                                    var paramUnifiedAuthorize = _SMData.GetParams(new StringValue() { Value = nameof(ParamsAuthorize.UnifiedAuthorize) }, cancellationToken: Context.ConnectionAborted);
                                    if (bool.TryParse(paramUnifiedAuthorize?.Value, out var unifiedAuthorize) && unifiedAuthorize)
                                    {
                                        Clients.OthersInGroup(ipConnect).Fire_RefreshToken(user.Token).Wait();
                                    }
                                }
                                response = user.Token;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(response))
                    {
                        _logger.LogTrace("Обновление токена завершилась неудачей");
                        await _tokens.RemoveTokenForIpAddress(httpContext.GetHeaderAppId());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(RefreshUser));
            }
            SemaphoreRelease();
            return response;
        }

        public async Task<bool> RefreshToken(string token)
        {
            try
            {
                _logger.LogTrace("Установка общего токена");
                var httpContext = Context.GetHttpContext();
                if (httpContext != null)
                {
                    await _tokens.UpdateTokenForAppId(httpContext.GetHeaderAppId(), token);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(RefreshToken));
            }
            return false;
        }


        public async Task SetLastActive()
        {
            try
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(3));

                var user = await _tokens.UpdateLastActiveForContextId(Context.ConnectionId);
                if (user != null)
                {
                    _logger.LogTrace("Установка активности для пользователя {name} ip адрес подключения {ip}", user.UserName, user.IpAddress);
                    user.LastActive = DateTimeOffset.UtcNow;
                    //единая авторизация
                    var paramUnifiedAuthorize = _SMData.GetParams(new StringValue() { Value = nameof(ParamsAuthorize.UnifiedAuthorize) }, cancellationToken: Context.ConnectionAborted);
                    bool.TryParse(paramUnifiedAuthorize?.Value, out var unifiedAuthorize);
                    if (unifiedAuthorize)
                    {
                        await _tokens.UpdateLastActiveForIp(user.IpAddress, user.UserName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetLastActive));
            }
            SemaphoreRelease();
        }

        void SemaphoreRelease()
        {
            if (semaphore.CurrentCount == 0)
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Проверяем аутентификацию пользователя и IP
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>       
        public async Task<string?> CheckAuthenticationApp(string? token)
        {
            string? response = null;
            try
            {
                var isAccessBegin = await _checkingIpResolution.CheckWhiteList();

                if (isAccessBegin.IsWhite && isAccessBegin.ArmWhiteType == SMSSGsoProto.V1.ArmWhiteType.ArmWhiteSupervisor && !(await CallerIsPods(Context.ConnectionAborted)))
                {
                    isAccessBegin.IsWhite = false;
                }

                if (isAccessBegin.IsWhite)
                {
                    var httpContext = Context.GetHttpContext();
                    if (httpContext != null)
                    {
                        var ipConnect = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                        //единая авторизация
                        var paramUnifiedAuthorize = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.UnifiedAuthorize) }, cancellationToken: Context.ConnectionAborted);
                        bool.TryParse(paramUnifiedAuthorize?.Value, out var unifiedAuthorize);

                        string forCheckToken = string.Empty;
                        bool isLogout = false;

                        try
                        {
                            await semaphore.WaitAsync(TimeSpan.FromSeconds(3));

                            var currentToken = await _tokens.GetTokenForAppId(httpContext.GetHeaderAppId());

                            _logger.LogTrace("Проверка авторизации приложения для ip адреса {ip}, тип ip адреса {type}", ipConnect, isAccessBegin.ArmWhiteType);
                            _logger.LogTrace("Включена единая авторизация {unifiedAuthorize}", unifiedAuthorize);
                            //включена единая авторизация
                            if (unifiedAuthorize && isAccessBegin.ArmWhiteType != SMSSGsoProto.V1.ArmWhiteType.ArmWhiteSupervisor)
                            {
                                //ищем общий токен
                                var sharedToken = await _tokens.FindSharedTokenForIp(ipConnect, httpContext.GetHeaderAppId());

                                if (sharedToken == null && !string.IsNullOrEmpty(currentToken?.UserToken) && currentToken.UserToken.Equals(token))
                                {
                                    forCheckToken = token;
                                }
                                else if (sharedToken != null)
                                {
                                    if (sharedToken.UserToken.Equals(token))
                                    {
                                        _logger.LogTrace("Входящий токен совпадает с общим");
                                        forCheckToken = token;
                                    }
                                    else
                                    {
                                        _logger.LogTrace("Установка общего токена");
                                        forCheckToken = sharedToken.UserToken;
                                    }
                                    await _tokens.AddTokenForAppId(httpContext.GetHeaderAppId(), new UserInfoConnect(sharedToken.UserName, sharedToken.UserToken, sharedToken.RefreshToken, ipConnect, Context.ConnectionId));
                                }
                                else
                                {
                                    _logger.LogTrace("Токен не действителен");
                                    isLogout = true;
                                    await _tokens.RemoveTokenForIpAddress(ipConnect);
                                }
                            }
                            else if (!string.IsNullOrEmpty(currentToken?.UserToken) && currentToken.UserToken.Equals(token))
                            {
                                forCheckToken = token;
                            }

                            if (!isLogout && await CheckToken(forCheckToken))
                            {
                                _logger.LogTrace("Проверка токена пройдена успешно, ip адрес подключения {ip}", ipConnect);
                                response = forCheckToken;
                            }
                            else
                            {
                                isLogout = true;
                            }
                        }
                        finally
                        {
                            SemaphoreRelease();
                        }

                        if (string.IsNullOrEmpty(response) || (isLogout && !string.IsNullOrEmpty(token)))
                        {
                            _logger.LogTrace("Проверка токена не пройдена, выход пользователя(ей), ip адрес подключения {ip}", ipConnect);

                            await Logout();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                SemaphoreRelease();
                _logger.WriteLogError(ex, nameof(CheckAuthenticationApp));
            }
            return response;
        }

        async Task<bool> CheckToken(string token)
        {
            try
            {
                if (JwtParser.IsValidToken(token))
                {
                    var sessIdValue = JwtParser.ParseIEnumerableClaimsFromJwt(token).FirstOrDefault(x => x.Type == nameof(AuthorizUser.UserSessID));
                    if (int.TryParse(sessIdValue?.Value, out var sessId))
                    {
                        var statusSess = await _SMGso.GetUserSessStatusAsync(new Int32Value() { Value = sessId });
                        if (statusSess.Value == 1)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CheckToken));
            }
            return false;
        }

        public async Task Logout()
        {
            try
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(3));
                var httpContext = Context.GetHttpContext();
                if (httpContext != null)
                {
                    var paramUnifiedAuthorize = await _SMData.GetParamsAsync(new StringValue() { Value = nameof(ParamsAuthorize.UnifiedAuthorize) }, cancellationToken: Context.ConnectionAborted);
                    bool.TryParse(paramUnifiedAuthorize?.Value, out var unifiedAuthorize);
                    var ipConnect = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";

                    var currentUser = await _tokens.GetTokenForAppId(httpContext.GetHeaderAppId());
                    if (currentUser != null)
                    {
                        _logger.LogTrace("Выход пользователя {name}, ip адрес подключения {ip}", currentUser?.UserName, currentUser?.IpAddress);
                        //единая авторизация

                        await _tokens.RemoveTokenForAppId(httpContext.GetHeaderAppId());
                    }

                    if (unifiedAuthorize)
                    {
                        _logger.LogTrace("Включена единая авторизация, удаление токенов для всех пользователей с IP {ip}", ipConnect);
                        await _tokens.RemoveTokenForIpAddress(ipConnect);
                    }

                    if (unifiedAuthorize)
                    {
                        await Clients.OthersInGroup(ipConnect).Fire_RemoveToken("единая авторизация");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка выхода пользователя {message}", ex.Message);
            }
            SemaphoreRelease();
        }

        /// <summary>
        /// Входящие подключение к серверу
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var context = Context.GetHttpContext();
                var urlConnect = context?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                StringValues valuesUi = new();
                var idUi = context?.Request.Query.TryGetValue(nameof(CookieName.AppId), out valuesUi);
                Guid.TryParse(valuesUi.FirstOrDefault(), out var guidUi);
                if (urlConnect != null)
                {
                    AddToGroup(urlConnect).Wait();
                }
                await semaphore.WaitAsync(TimeSpan.FromSeconds(3));

                await _tokens.UpdateContextId(context?.GetHeaderAppId(), Context.ConnectionId);

                _logger.LogTrace(@"Новое подключение к серверу авторизации от {forUrl}, guid {guid}, назначенный ID {Id}, локальный хост {Host}", urlConnect, guidUi, Context.ConnectionId, context?.Request.Host);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(OnConnectedAsync));
            }
            SemaphoreRelease();
        }

        /// <summary>
        /// Потеря подключения к серверу
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception? OnDisconnectedAsync)
        {
            try
            {
                var context = Context.GetHttpContext();
                var urlConnect = context?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                StringValues valuesUi = new();
                var idUi = context?.Request.Query.TryGetValue(nameof(CookieName.AppId), out valuesUi);
                Guid.TryParse(valuesUi.FirstOrDefault(), out var guidUi);
                if (urlConnect != null)
                {
                    RemoveFromGroup(urlConnect).Wait();
                }
                _logger.LogTrace(@"Закрыто подключение к серверу авторизации для {forUrl}, guid {guid}, назначенный ID {Id}, локальный хост {Host}", urlConnect, guidUi, Context.ConnectionId, context?.Request.Host);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(OnDisconnectedAsync));
            }
            return base.OnDisconnectedAsync(OnDisconnectedAsync);
        }


        async Task AddToGroup(string groupName)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(AddToGroup));
            }
        }

        async Task RemoveFromGroup(string groupName)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(RemoveFromGroup));
            }
        }

    }
}

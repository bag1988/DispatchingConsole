using System.Text;
using System.Text.RegularExpressions;
using Asp.Versioning;
using BlazorLibrary.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using SensorM.GsoCommon.ServerLibrary.Models;
using SensorM.GsoCore.RemoteConnectLibrary;
using SensorM.GsoCore.SharedLibrary.Interfaces;
using ServerLibrary.Extensions;
using SharedLibrary.Models;
using SharedLibrary.Utilities;

namespace DispatchingConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/chat/[action]")]
    [AllowAnonymous]
    public class CommunicationController : ControllerBase, IChatHub
    {
        private readonly ILogger<CommunicationController> _logger;
        static HubConnection? _hubConnection;

        readonly RemoteHttpProvider _httpClient;

        readonly string DirectoryTmp;

        public CommunicationController(ILogger<CommunicationController> logger, IConfiguration conf, RemoteHttpProvider httpClient)
        {
            _logger = logger;
            try
            {
                if (_hubConnection == null)
                {
                    var url = conf.GetValue<string?>("Kestrel:Endpoints:Http:Url")?.Split(":").LastOrDefault() ?? "8080";
                    if (!string.IsNullOrEmpty(url) && int.TryParse(url, out var port))
                    {
                        _hubConnection = new HubConnectionBuilder().WithUrl(new Uri($"http://127.0.0.1:{port}/CommunicationChatHub")).Build();
                        _hubConnection.SubscribeViaInterface(this, typeof(IChatHub));
                    }
                }
                DirectoryTmp = conf.GetValue<string?>("PodsArhivePath") ?? "PodsArhivePath";
            }
            catch (Exception ex)
            {
                DirectoryTmp = "PodsArhivePath";
                _logger.LogError("Ошибка инитиализации {error}", ex.Message);
            }
            _httpClient = httpClient;
        }

        async Task SendHubConnect<TData>(string method, TData model)
        {
            await SendHubConnect(method, [model]);
        }
        async Task SendHubConnect(string method, object?[] args)
        {
            try
            {
                _logger.LogTrace("Удаленный вызов от: {remote} к методу: {method}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", method);
                if (_hubConnection != null)
                {
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StartAsync();
                    }
                    _logger.LogTrace("Вызов метода: {method}, состояние подключения к хабу: {state}", method, _hubConnection.State);
                    if (_hubConnection.State == HubConnectionState.Connected)
                    {
                        await _hubConnection.SendCoreAsync(method, args);
                    }
                    else
                    {
                        _logger.LogTrace("Ошибка пересылки данных в hub, состояние подключения {state}", _hubConnection.State);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Удаленный вызов от: {remote} к методу: {method} завершился ошибкой: {error}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", method, ex.Message);
            }
        }

        async Task<TResult?> InvokeHubConnect<TResult>(string method, object?[] args)
        {
            try
            {
                _logger.LogTrace("Удаленный вызов от: {remote} к методу: {method}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", method);
                if (_hubConnection != null)
                {
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StartAsync();
                    }
                    _logger.LogTrace("Вызов метода: {method}, состояние подключения к хабу: {state}", method, _hubConnection.State);
                    if (_hubConnection.State == HubConnectionState.Connected)
                    {
                        return await _hubConnection.InvokeCoreAsync<TResult>(method, args);
                    }
                    else
                    {
                        _logger.LogTrace("Ошибка пересылки данных в hub, состояние подключения {state}", _hubConnection.State);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Удаленный вызов от: {remote} к методу: {method} завершился ошибкой: {error}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", method, ex.Message);
            }
            return default;
        }


        [HttpGet]
        public FileResult? GetVideoServer([FromQuery] string fileName)
        {
            try
            {
                _logger.LogTrace("Удаленный вызов от: {remote} к методу: {method}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", nameof(GetVideoServer));
                fileName = Path.Combine(DirectoryTmp, fileName);

                if (System.IO.File.Exists(fileName))
                {
                    Response.Headers.ContentDisposition = $"attachment;filename=\"{System.Net.WebUtility.UrlEncode(Path.GetFileName(fileName))}\"";
                    return PhysicalFile(fileName, "video/webm");
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
            return null;
        }

        [HttpGet]
        public FileResult? GetAudioServer([FromQuery] string fileName)
        {
            try
            {
                _logger.LogTrace("Удаленный вызов от: {remote} к методу: {method}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", nameof(GetAudioServer));
                fileName = Path.Combine(DirectoryTmp, fileName);
                if (System.IO.File.Exists(fileName))
                {
                    return PhysicalFile(fileName, "audio/mpeg");
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
            return null;
        }

        [HttpGet]
        public FileResult? GetFileServer([FromQuery] string fileName)
        {
            try
            {
                _logger.LogTrace("Удаленный вызов от: {remote} к методу: {method}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", nameof(GetFileServer));
                fileName = Path.Combine(DirectoryTmp, fileName);
                if (System.IO.File.Exists(fileName))
                {
                    return PhysicalFile(fileName, "application/octet-stream");
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
            return null;
        }

        [HttpGet]
        public FileResult? DownLoadFile([FromQuery] string fileName)
        {
            try
            {
                _logger.LogTrace("Удаленный вызов от: {remote} к методу: {method}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", nameof(DownLoadFile));
                fileName = Path.Combine(DirectoryTmp, fileName);

                if (System.IO.File.Exists(fileName))
                {
                    return PhysicalFile(fileName, "application/octet-stream");
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
            return null;
        }

        [HttpPost]
        public async Task<IActionResult> ReplicateFiles(ReplicateFilesRequest request)
        {
            try
            {
                _logger.LogTrace("Удаленный вызов от: {remote} к методу: {method}", HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "нет ip адреса", nameof(ReplicateFiles));
                if (!string.IsNullOrEmpty(request.FileUrl) && request.UserNames?.Length > 0 && request.KeyChatRoom != null && !string.IsNullOrEmpty(request.Message) && !string.IsNullOrEmpty(request.RemoteUrl?.UserName))
                {

                    var authorityUrl = IpAddressUtilities.GetAuthority(request.RemoteUrl.AuthorityUrl);

                    if (!string.IsNullOrEmpty(authorityUrl))
                    {
                        Stream? fileStream = null;

                        var absoluteUri = $"https://{authorityUrl}";
                        try
                        {
                            using var result = await _httpClient.GetAsync(absoluteUri, "api/v1/chat/DownLoadFile", $"fileName={request.FileUrl}");
                            if (result.IsSuccessStatusCode)
                            {
                                fileStream = await result.Content.ReadAsStreamAsync();
                                if (fileStream != null)
                                {
                                    var message = new ChatMessage(request.RemoteUrl.AuthorityUrl, request.RemoteUrl.UserName, request.Message);

                                    var firstUser = request.UserNames[0];
                                    var filePath = "";
                                    Regex regexUserName = new(@"[^\w]");
                                    if (!string.IsNullOrEmpty(firstUser))
                                    {
                                        filePath = Path.Combine(regexUserName.Replace(Convert.ToBase64String(Encoding.UTF8.GetBytes(firstUser)), ""), $"{request.KeyChatRoom}");
                                    }
                                    filePath = Path.Combine(filePath, Path.ChangeExtension(Path.GetRandomFileName(), Path.GetExtension(request.FileUrl)));

                                    var writePath = Path.Combine(DirectoryTmp, filePath);

                                    var dir = Path.GetDirectoryName(writePath);
                                    if (!string.IsNullOrEmpty(dir))
                                    {
                                        if (!Directory.Exists(dir))
                                        {
                                            Directory.CreateDirectory(dir);
                                        }
                                        if (!System.IO.File.Exists(writePath))
                                        {
                                            using (var fs = new FileStream(writePath, FileMode.Create, FileAccess.Write, FileShare.Read, 1_000_000))
                                            {
                                                var readCount = 0;
                                                byte[] buffer = new byte[1_000_000];
                                                while ((readCount = await fileStream.ReadAsync(buffer)) > 0)
                                                {
                                                    await fs.WriteAsync(buffer.Take(readCount).ToArray());
                                                }
                                            }
                                            await fileStream.DisposeAsync();
                                            message.Url = filePath;
                                            await SendHubConnect("AddMessageForUser", [IpAddressUtilities.GetAuthority(Request.Host.Value), firstUser, request.KeyChatRoom, message]);
                                        }
                                        var otherUser = request.UserNames.Skip(1);
                                        if (otherUser?.Any() ?? false)
                                        {
                                            await SendHubConnect("ReplicateFiles", [otherUser, request.KeyChatRoom, IpAddressUtilities.GetAuthority(Request.Host.Value), message]);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLogError(ex, $"{Request.RouteValues["action"]?.ToString()} for {absoluteUri}");
                        }
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetLocalContact()
        {
            try
            {
                var response = await InvokeHubConnect<IEnumerable<ContactInfo>?>("GetLocalContact", Array.Empty<object>());
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChatRoom([FromBody] JoinModel model)
        {
            try
            {
                await SendHubConnect("CreateChatRoom", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> JoinToChatRoom([FromBody] JoinModel model)
        {
            try
            {
                await SendHubConnect("JoinToChatRoom", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> AnswerForJoinChatRoom([FromBody] AnswerForJoin model)
        {
            try
            {
                await SendHubConnect("AnswerForJoinChatRoom", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> GoConnectionP2P([FromBody] KeyChatForUrl model)
        {
            try
            {
                await SendHubConnect("GoConnectionP2P", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> SetRemoteOfferForUrl([FromBody] KeyChatForUrlAndValue model)
        {
            try
            {
                await SendHubConnect("SetRemoteOfferForUrl", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> SetAnswerForRemoteClient([FromBody] KeyChatForUrlAndValue model)
        {
            try
            {
                await SendHubConnect("SetAnswerForRemoteClient", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> SendCandidate([FromBody] KeyChatForUrlAndValue model)
        {
            try
            {
                await SendHubConnect("SendCandidate", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> CancelOutCallRemote([FromBody] KeyChatForUrl model)
        {
            try
            {
                await SendHubConnect("CancelOutCallRemote", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> CancelCallRemote([FromBody] KeyChatForUrl model)
        {
            try
            {
                await SendHubConnect("CancelCallRemote", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> CloseCall([FromBody] KeyChatForUrl model)
        {
            try
            {
                await SendHubConnect("CloseCall", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        public async Task<IActionResult> SetMessage([FromBody] KeyChatForUrlAndValue model)
        {
            try
            {
                await SendHubConnect("SetMessage", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetChangeRemoteStream([FromBody] KeyChatForUrlAndValue model)
        {
            try
            {
                await SendHubConnect("SetChangeRemoteStream", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetAddItemsForChatRoom([FromBody] JoinModel model)
        {
            try
            {
                await SendHubConnect("SetAddItemsForChatRoom", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetDeleteItemsForChatRoom([FromBody] JoinModel model)
        {
            try
            {
                await SendHubConnect("SetDeleteItemsForChatRoom", model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}

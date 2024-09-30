using Microsoft.Extensions.Logging;
using SharedLibrary.Utilities;
using System.Net.Sockets;

namespace SensorM.GsoCore.RemoteConnectLibrary
{
    public class RemoteHttpProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RemoteHttpProvider> _logger;
        public RemoteHttpProvider(IHttpClientFactory httpClientFactory, ILogger<RemoteHttpProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private async Task<bool> CheckConnect(string baseUrl, CancellationToken cancellationToken = default)
        {
            bool IsHaveConnect = false;
            try
            {
                var host = IpAddressUtilities.GetHost(baseUrl);
                using (var testConnect = new TcpClient())
                {
                    var result = testConnect.BeginConnect(host, IpAddressUtilities.ParseUri(baseUrl).Port, null, null);
                    int countWait = 200;
                    while (countWait > 0 && !testConnect.Connected && !cancellationToken.IsCancellationRequested)
                    {
                        countWait--;
                        await Task.Delay(10, cancellationToken);
                    }
                    IsHaveConnect = testConnect.Connected;
                    testConnect.EndConnect(result);
                    testConnect.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("CheckConnect {url} - {error}", baseUrl, ex.Message);
            }
            return IsHaveConnect;
        }

        public async Task<HttpResponseMessage> PostAsync(string baseUrl, string methodName, HttpContent? model, HttpCompletionOption? httpCompletion = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
        {
            bool IsHaveConnect = true;// await CheckConnect(baseUrl, cancellationToken);

            if (IsHaveConnect)
            {
                try
                {
                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{methodName}");
                    httpRequestMessage.Content = model;
                    var httpClient = _httpClientFactory.CreateClient(nameof(RemoteHttpProvider));
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletion ?? HttpCompletionOption.ResponseContentRead, cancellationToken);
                    return httpResponseMessage;
                }
                catch (Exception ex)
                {
                    _logger.LogTrace("{url}/{methodName} - {error}", baseUrl, methodName, ex.Message);
                }
            }
            //else
            //{
            //    _logger.LogTrace("{url} ошибка подключения", baseUrl);
            //}

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }

        public async Task<HttpResponseMessage> GetAsync(string baseUrl, string methodName, string? query, HttpCompletionOption? httpCompletion = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
        {
            bool IsHaveConnect = true;// await CheckConnect(baseUrl, cancellationToken);
            if (IsHaveConnect)
            {
                try
                {
                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{methodName}{(!string.IsNullOrEmpty(query) ? $"?{query}" : "")}");
                    var httpClient = _httpClientFactory.CreateClient(nameof(RemoteHttpProvider));
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletion ?? HttpCompletionOption.ResponseContentRead, cancellationToken);
                    return httpResponseMessage;
                }
                catch (Exception ex)
                {
                    _logger.LogTrace("{url}/{methodName} - {error}", baseUrl, methodName, ex.Message);
                }
            }
            //else
            //{
            //    _logger.LogTrace("{url} ошибка подключения", baseUrl);
            //}

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }

    }
}

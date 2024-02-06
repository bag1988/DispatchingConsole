using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SharedLibrary.Utilities;

namespace SensorM.GsoCommon.ServerLibrary
{
    public class CreateHttpClient
    {
        readonly Dictionary<string, HttpClient> _httpClients = new();
        
        public HttpClient? GetHttpClient(string forUrl)
        {
            try
            {
                lock (_httpClients)
                {
                    var host = IpAddressUtilities.ParseUri(forUrl).Host;

                    if (_httpClients.ContainsKey(forUrl))
                    {
                        return _httpClients[forUrl];
                    }
                    else if (!string.IsNullOrEmpty(host))
                    {
                        var handler = new SocketsHttpHandler
                        {
                            SslOptions = new() { RemoteCertificateValidationCallback = delegate { return true; } },
                            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
                        };
                        var testConnect = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        var b = testConnect.ConnectAsync(host, IpAddressUtilities.ParseUri(forUrl).Port).Wait(1000);
                        testConnect.Close();
                        testConnect.Dispose();
                        if (b)
                        {
                            var httpClient = new HttpClient(handler);
                            httpClient.BaseAddress = new Uri(forUrl);
                            _httpClients.Add(forUrl, httpClient);
                            return httpClient;
                        }
                    }
                }
                return null;
            }
            catch 
            {
                throw;
            }
        }

        public void RemoveClient(string forUrl)
        {
            lock (_httpClients)
            {
                if (_httpClients.ContainsKey(forUrl))
                {
                    _httpClients[forUrl].Dispose();
                    _httpClients.Remove(forUrl);
                }
            }
        }

        public void RemoveAll()
        {
            lock (_httpClients)
            {
                foreach (var client in _httpClients)
                {
                    try
                    {
                        client.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка получения клиента для {url}, {message}", client.Key, ex.Message);
                    }
                }
                _httpClients.Clear();
            }

        }

    }
}

using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using static GateServiceProto.V1.GateService;

namespace RemoteConnectLibrary
{
    public class RemoteGateProvider
    {
        private readonly Dictionary<string, GateClients> _clients = new();
        readonly ILogger<RemoteGateProvider> _logger;

        public RemoteGateProvider(ILogger<RemoteGateProvider> logger)
        {
            _logger = logger;
        }

        private class GateClients
        {
            public GateClients(GrpcChannel channel, Metadata? metaDataClient)
            {
                Channel = channel;
                MetaDataClient = metaDataClient;
            }

            public GrpcChannel Channel { get; init; }

            public Metadata? MetaDataClient { get; set; }
        }

        private GrpcChannel CreateChannel(string baseUri)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(baseUri, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler()
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = Timeout.InfiniteTimeSpan,
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                },
                UnsafeUseInsecureChannelCallCredentials = true
            });

            return channel;
        }

        private CallInvoker GetCallInvoker(GrpcChannel channel, string baseUri)
        {
            var c = channel.Intercept((request) => ReplaceMetadata(request, _clients.ContainsKey(baseUri) ? _clients[baseUri].MetaDataClient : new() { new(MetaDataName.DaprAppId, DaprNameService.ServerGRPCSMGATE) }));
            return c;
        }

        public void AddMetaData(string baseUri, Metadata? metaData = null)
        {
            if (metaData == null || !_clients.ContainsKey(baseUri)) return;
            var oldData = _clients[baseUri].MetaDataClient ?? new();
            lock (oldData)
            {
                _clients[baseUri].MetaDataClient = ReplaceMetadata(metaData, oldData);
            }
        }

        public GateServiceClient? GetGateClient(string baseUri, Metadata? metaData = null)
        {
            lock (_clients)
            {
                try
                {
                    if (!_clients.ContainsKey(baseUri))
                    {
                        GrpcChannel channel = CreateChannel(baseUri);

                        if (!metaData?.Any(x => x.Key == MetaDataName.DaprAppId) ?? true)
                        {
                            if (metaData == null)
                                metaData = new();
                            metaData.Add(new(MetaDataName.DaprAppId, DaprNameService.ServerGRPCSMGATE));
                        }

                        if (metaData.All(x => x.Key != MetaDataName.DaprStream))
                        {
                            metaData.Add(new Metadata.Entry(MetaDataName.DaprStream, "true"));
                        }

                        GateClients client = new(channel, metaData);

                        _clients.Add(baseUri, client);
                    }
                    else
                    {
                        AddMetaData(baseUri, metaData);
                    }

                    var connect = _clients[baseUri].Channel.ConnectAsync(default).Wait(1000);

                    _logger.LogTrace(@"{url} состояние канала: {state}", baseUri, _clients[baseUri].Channel.State);
                    if (_clients[baseUri].Channel.State == ConnectivityState.Ready)
                    {
                        return new GateServiceClient(GetCallInvoker(_clients[baseUri].Channel, baseUri));
                    }
                }
                catch (Exception ex)
                {
                    DisposeChannel(baseUri);
                    _logger.LogError(@"{url} ошибка создания клиента: {message} ", baseUri, ex.Message);
                }
                return null;
            }
        }

        public async Task<GateServiceClient?> AuthorizeRemote(string baseUri, string login, string password, CancellationToken token)
        {
            if (_clients.ContainsKey(baseUri))
            {
                var metaData = _clients[baseUri].MetaDataClient;

                if (metaData?.Any(x => x.Key == MetaDataName.Authorization) ?? false)
                {
                    var valueBearer = metaData.First(x => x.Key == MetaDataName.Authorization).Value;

                    if (valueBearer.Split(' ').Length > 0 && JwtParser.IsValidToken(valueBearer.Split(' ')[1]) && _clients[baseUri].Channel.State == ConnectivityState.Ready)
                    {
                        return new GateServiceClient(GetCallInvoker(_clients[baseUri].Channel, baseUri));
                    }
                }
            }

            try
            {
                var client = GetGateClient(baseUri);
                if (client != null)
                {
                    var s = await client.BeginUserSessAsync(new RequestLogin() { User = login, Password = password }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: token);

                    if (!string.IsNullOrEmpty(s.Token))
                    {
                        var data = new Metadata() { new Metadata.Entry(MetaDataName.Authorization, $"{MetaDataName.Bearer} {s.Token}") };
                        AddMetaData(baseUri, data);
                        return client;
                    }
                }
            }
            catch (Exception ex)
            {
                DisposeChannel(baseUri);
                _logger.LogError(@"{url} ошибка авторизации, логин: {login}, пароль: {password}, ошибка: {message} ", baseUri, login, password, ex.Message);
            }
            return null;
        }

        public void DisposeChannel(string? url)
        {
            if (!string.IsNullOrEmpty(url) && _clients.ContainsKey(url))
            {
                _clients[url].Channel.Dispose();
                _clients.Remove(url);
            }
        }

        public Metadata ReplaceMetadata(Metadata newData, Metadata? oldData)
        {
            if (oldData == null)
            {
                return newData;
            }

            foreach (var item in newData)
            {
                if (oldData.Any(x => x.Key == item.Key))
                {
                    var elem = oldData.First(x => x.Key == item.Key);
                    if (elem.Value != item.Value)
                    {
                        oldData.Remove(elem);
                        oldData.Add(item);
                    }
                }
                else
                    oldData.Add(item);
            }
            return oldData;
        }
    }
}

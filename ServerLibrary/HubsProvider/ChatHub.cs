using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using PodsProto.V1;
using LocalizationLibrary;
using SensorM.GsoCore.RemoteConnectLibrary;
using SensorM.GsoCore.SharedLibrary.Interfaces;
using ServerLibrary.Extensions;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;
using ChatForUser = SharedLibrary.Models.ChatForUser;
using ChatInfo = SharedLibrary.Models.ChatInfo;
using ChatMessage = SharedLibrary.Models.ChatMessage;
using ConnectInfo = SharedLibrary.Models.ConnectInfo;
using StateCall = SharedLibrary.Models.StateCall;
using TypeConnect = SharedLibrary.Models.TypeConnect;
using SensorM.GsoCommon.ServerLibrary.Models;

namespace ServerLibrary.HubsProvider
{
    [AllowAnonymous]
    public class ChatHub : Hub<IChatHub>
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly static ConnectionMapping _connections = new();
        private readonly RemoteHttpProvider _httpClient;
        readonly PodsProto.V1.PodsService.PodsServiceClient _pods;
        readonly SMSSGsoClient _SMSGso;
        readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        readonly StaffDataClient _StaffData;

        readonly IStringLocalizer<DispatchingLocalization> DispRep;

        /// <summary>
        /// Пропущенный вызов, ключ имя пользователя
        /// </summary>
        readonly static Dictionary<string, ChatInfo> MissedCall = new();

        /// <summary>
        /// Текущие подключение (Identity.Name, Request.Host)
        /// </summary>
        MyConnectInfo? _getMyKeyConnect
        {
            get
            {
                try
                {
                    return _connections.GetMyConnect(Context.ConnectionId);
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, nameof(_getMyKeyConnect));
                }
                return null;
            }
        }

        /// <summary>
        /// Список чатов для пользователя
        /// </summary>
        static List<ChatForUser> AllChats { get; } = new();

        readonly string DirectoryTmp;


        public ChatHub(ILogger<ChatHub> logger, IStringLocalizer<DispatchingLocalization> dispRep, SMSSGsoClient sMSGso, StaffDataClient staffData, SMDataService.SMDataServiceClient sMData, PodsProto.V1.PodsService.PodsServiceClient pods, IConfiguration conf, RemoteHttpProvider httpClient)
        {
            _logger = logger;
            DispRep = dispRep;
            _SMSGso = sMSGso;
            _StaffData = staffData;
            _SMData = sMData;
            _pods = pods;

            DirectoryTmp = conf.GetValue<string?>("PodsArhivePath") ?? "PodsArhivePath";
            _httpClient = httpClient;
        }

        PodsProto.V1.ChatInfo ToProtoModel(ChatInfo model)
        {
            PodsProto.V1.ChatInfo chatInfo = new PodsProto.V1.ChatInfo();
            chatInfo.Key = model.Key.ToString();
            chatInfo.NameRoom = model.NameRoom;
            chatInfo.Items.AddRange(model.Items.Select((ConnectInfo x) => new PodsProto.V1.ConnectInfo
            {
                AuthorityUrl = x.AuthorityUrl,
                UserName = x.UserName,
                State = PodsProto.V1.StateCall.Disconnect
            }));
            chatInfo.IsDefault = model.IsDefault;
            chatInfo.UserCreate = model.UserCreate;
            chatInfo.IdUiConnect = model.IdUiConnect.ToString();
            chatInfo.AuthorityCreate = model.AuthorityCreate;
            return chatInfo;
        }

        ChatInfo ToChatInfo(PodsProto.V1.ChatInfo other)
        {
            Guid.TryParse(other.Key, out var result);

            ChatInfo response = new()
            {
                Key = result,
                NameRoom = other.NameRoom,
                IsDefault = other.IsDefault,
                UserCreate = other.UserCreate,
                AuthorityCreate = IpAddressUtilities.GetAuthority(other.AuthorityCreate)
            };
            response.Items.AddRange(other.Items.Select((PodsProto.V1.ConnectInfo x) => new ConnectInfo(x.AuthorityUrl, x.UserName)));
            if (Guid.TryParse(other.IdUiConnect, out var result2))
            {
                response.IdUiConnect = result2;
            }
            return response;
        }

        public async Task<bool> UploadFile(ChannelReader<byte[]> stream, string FileName)
        {
            using var activity = this.ActivitySourceForHub()?.StartActivity();
            activity?.AddTag("Новый файл", FileName);
            FileName = Path.Combine(DirectoryTmp, FileName);
            try
            {
                var dir = Path.GetDirectoryName(FileName);
                if (string.IsNullOrEmpty(dir))
                    return false;

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (System.IO.File.Exists(FileName))
                    return false;
                using (var fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read, 1_000_000))
                {
                    while (await stream.WaitToReadAsync(Context.ConnectionAborted))
                    {
                        while (stream.TryRead(out var item))
                        {
                            await fs.WriteAsync(item, Context.ConnectionAborted);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(FileName))
                    System.IO.File.Delete(FileName);
                _logger.WriteLogError(ex, nameof(UploadFile));
                return false;
            }
        }

        public async Task<Dictionary<Guid, int>?> GetCountNoReadMessages()
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    var newData = await _pods.GetCountNoReadMessagesAsync(new PodsProto.V1.UserIdentity() { AuthorityUrl = _getMyKeyConnect.AuthorityUrl, UserName = _getMyKeyConnect.UserName }, cancellationToken: Context.ConnectionAborted);
                    if (newData?.List.Count > 0)
                    {
                        return newData.List.ToDictionary(x => (Guid.TryParse(x.ChatKey, out var key) ? key : Guid.Empty), x => x.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GetCountNoReadMessages));
            }
            return null;
        }


        /// <summary>
        /// Возвращаем список чатов где содержится данный пользователь
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ChatInfo>?> GetConnectionList()
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    var newData = await _pods.GetChatsAsync(new PodsProto.V1.UserIdentity() { AuthorityUrl = _getMyKeyConnect.AuthorityUrl, UserName = _getMyKeyConnect.UserName }, cancellationToken: Context.ConnectionAborted);
                    if (newData?.List.Count > 0)
                    {
                        var addData = newData.List.Select(ToChatInfo);
                        lock (AllChats)
                        {
                            var curentUserChats = FindChatListForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName);
                            var exeptList = addData.ExceptBy(curentUserChats.Select(f => f.Key), x => x.Key).ToList();
                            var deletetList = curentUserChats.ExceptBy(addData.Select(f => f.Key), x => x.Key).ToList();

                            AllChats.First(x => x.UserName == _getMyKeyConnect.UserName && IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)).Chats.AddRange(exeptList);

                            if (deletetList?.Count > 0)
                            {
                                AllChats.First(x => x.UserName == _getMyKeyConnect.UserName && IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)).Chats.RemoveAll(deletetList.Contains);
                            }
                        }
                        return addData;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GetConnectionList));
            }

            return null;
        }

        /// <summary>
        /// Поиск чата для пользователя по ключу
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        ChatInfo? FindChatForKey(string userName, Guid keyChatRoom)
        {
            if (AllChats.Any(x => x.UserName == userName))
            {
                if (AllChats.First(x => x.UserName == userName).Chats.Any(x => x.Key == keyChatRoom))
                {
                    return AllChats.First(x => x.UserName == userName).Chats.First(x => x.Key == keyChatRoom);
                }
            }
            return null;
        }

        ChatInfo? FindChatForKey(string? userName, string? authorityUrl, Guid? keyChatRoom)
        {
            if (AllChats.Any(x => x.UserName == userName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl)))
            {
                if (AllChats.First(x => x.UserName == userName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl)).Chats.Any(x => x.Key == keyChatRoom))
                {
                    return AllChats.First(x => x.UserName == userName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl)).Chats.First(x => x.Key == keyChatRoom);
                }
            }
            return null;
        }

        /// <summary>
        /// Поиск чата для пользователя по AuthorityUrl
        /// </summary>
        /// <param name="forUrl"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        IEnumerable<ChatInfo> FindChatListForUser(string? forUrl, string? userName)
        {
            if (!string.IsNullOrEmpty(forUrl) && !string.IsNullOrEmpty(userName) && !AllChats.Any(x => x.UserName == userName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl)))
                AllChats.Add(new ChatForUser(forUrl, userName));
            return AllChats.First(x => x.UserName == userName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl)).Chats;
        }

        /// <summary>
        /// Получить список чатов для текущего подключения
        /// </summary>
        IEnumerable<ChatInfo> GetChatListForUser
        {
            get
            {
                if (_getMyKeyConnect == null)
                    throw new ArgumentNullException();

                if (!string.IsNullOrEmpty(_getMyKeyConnect.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName) && !AllChats.Any(x => x.UserName == _getMyKeyConnect.UserName && IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)))
                    AllChats.Add(new(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName));
                return AllChats.First(x => x.UserName == _getMyKeyConnect.UserName && IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)).Chats;
            }
        }

        /// <summary>
        /// Получить историю чатов для текущего подключения
        /// </summary>
        /// <returns></returns>        
        public async Task<MessagesAndAllCount?> GetMessageForChat(Guid keyChatRoom, int skip = 0, int take = 100, byte[]? filtr = null)
        {
            if (!string.IsNullOrEmpty(_getMyKeyConnect?.UserName))
            {
                UserKeyAndChatKey model = new()
                {
                    ChatKey = keyChatRoom.ToString(),
                    UserKey = new()
                    {
                        AuthorityUrl = _getMyKeyConnect.AuthorityUrl,
                        UserName = _getMyKeyConnect.UserName
                    }
                };
                RequestPaginatedOutput request = new()
                {
                    Skip = skip,
                    Take = take,
                    Key = Any.Pack(model),
                    Filtr = Any.Parser.ParseFrom(filtr)
                };
                var result = await _pods.GetMessagesForChatAsync(request, cancellationToken: Context.ConnectionAborted);
                if (result?.List.Count > 0)
                {
                    return new MessagesAndAllCount()
                    {
                        Messages = result.List.Select(x => new ChatMessage()
                        {
                            AuthorityUrl = x.AuthorityUrl,
                            Date = x.Date.ToDateTime(),
                            Message = x.Message,
                            Url = x.Url,
                            UserName = x.UserName
                        }).ToList(),
                        AllCount = result.AllCount
                    };
                }
            }
            return null;
        }

        /// <summary>
        /// Получить список найденых пользователей
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SharedLibrary.Models.ContactInfo>?> GetAllContactForUser()
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl))
                {
                    var newList = await _pods.GetAllContactForUserAsync(new UserIdentity()
                    {
                        AuthorityUrl = _getMyKeyConnect.AuthorityUrl,
                        UserName = _getMyKeyConnect.UserName
                    });

                    if (newList?.List?.Count > 0)
                    {
                        var listAdd = newList.List.Select(x => new SharedLibrary.Models.ContactInfo(x.NameCu, x.AuthorityUrl, x.UserName, x.StaffId, x.LastActive?.ToDateTime()) { Type = (SharedLibrary.Models.TypeContact)x.Type });

                        return listAdd;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GetAllContactForUser));
            }
            return null;
        }

        /// <summary>
        /// Запрос пользователей с управляющего пункта
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SharedLibrary.Models.ContactInfo>?> GetLocalContact()
        {
            try
            {
                var resultList = await _pods.GetLocalContactAsync(new Empty());

                if (resultList?.List?.Count > 0)
                {
                    var responseList = resultList.List.Select(x => new SharedLibrary.Models.ContactInfo(x.NameCu, x.AuthorityUrl, x.UserName, x.StaffId, x.LastActive?.ToDateTime()) { Type = (SharedLibrary.Models.TypeContact)x.Type });
                    return responseList;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GetLocalContact));
            }
            return Enumerable.Empty<SharedLibrary.Models.ContactInfo>();
        }

        public async Task<string?> GetLocalAuthorityUrl()
        {
            try
            {
                var ipList = await GetLocalHostUrl();
                if (!string.IsNullOrEmpty(ipList))
                {
                    var portInfo = await _SMSGso.GetAppPortsAsync(new BoolValue() { Value = true });
                    return $"{ipList}:{portInfo?.DISPATCHINGCONSOLEAPPPORT}";
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GetLocalAuthorityUrl));
            }
            return null;
        }

        async Task<string?> GetLocalHostUrl()
        {
            try
            {
                var ipList = await _SMData.GetServerIPAddressesAsync(new Google.Protobuf.WellKnownTypes.Empty());
                return ipList?.Array.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GetLocalHostUrl));
            }
            return null;
        }

        async Task<IEnumerable<PodsProto.V1.ContactInfo>> GetLocalCuInfo()
        {
            try
            {
                var staffInfo = (await _StaffData.GetRegInfo1Async(new Null()))?.Array.FirstOrDefault();
                var ipList = await GetLocalHostUrl();
                var response = await _SMSGso.GetGsoUserEx2Async(new Null());
                if (response != null)
                {
                    var StaffId = staffInfo?.OBJID?.StaffID ?? 0;
                    var CuName = !string.IsNullOrEmpty(staffInfo?.CuName) ? staffInfo.CuName : !string.IsNullOrEmpty(staffInfo?.UNC) ? staffInfo.UNC : (ipList ?? "ошибка определения");
                    var AuthorityUrl = !string.IsNullOrEmpty(IpAddressUtilities.GetHost(staffInfo?.UNC)) ? IpAddressUtilities.GetHost(staffInfo?.UNC) : ipList ?? "0.0.0.0";

                    var activeUser = GetConnectUsers();
                    if (StaffId > 0)
                    {
                        var portInfo = await _SMSGso.GetAppPortsAsync(new BoolValue() { Value = true });
                        AuthorityUrl = $"{AuthorityUrl}:{portInfo?.DISPATCHINGCONSOLEAPPPORT}";
                        ContactInfoList request = new();
                        request.List.AddRange(response.Array.Select(item => new PodsProto.V1.ContactInfo()
                        {
                            NameCu = CuName,
                            AuthorityUrl = AuthorityUrl,
                            UserName = item.Login,
                            StaffId = item.OBJID?.StaffID ?? StaffId,
                            LastActive = (activeUser?.Contains(item.Login) ?? false) ? DateTime.UtcNow.ToTimestamp() : null,
                            Type = PodsProto.V1.TypeContact.Local
                        }));
                        return request.List;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GetLocalCuInfo));
            }
            return Enumerable.Empty<PodsProto.V1.ContactInfo>();
        }

        /// <summary>
        /// Получить пропущенный вызов для текущего подключения по ключу
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        public ChatInfo? GetMissedCall()
        {
            if (string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) || string.IsNullOrEmpty(_getMyKeyConnect.UserName)) return null;

            ChatInfo? response = null;
            if (MissedCall != null && MissedCall.ContainsKey(_getMyKeyConnect.UserName))
            {
                response = GetChatListForUser.FirstOrDefault(x => x.Key == MissedCall[_getMyKeyConnect.UserName].Key);

                MissedCall.Remove(_getMyKeyConnect.UserName);

                if (response?.Items.Any(x => IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl) && x.UserName == _getMyKeyConnect.UserName) ?? false)
                {
                    return response;
                }
            }
            return null;
        }

        /// <summary>
        /// Отправляем уведомление для удаления чата пользователю по ключу
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userName"></param>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        async Task DeleteChatForKey(string? url, string? userName, Guid keyChatRoom)
        {
            try
            {
                var r = _connections.GetContextIdForLogin(url, userName);
                if (r?.Count() > 0)
                {
                    await Clients.Clients(r).DeleteChatForKey(keyChatRoom);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(DeleteChatForKey));
            }
        }

        /// <summary>
        /// Отправляем уведомление для обновления чата пользователю
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userName"></param>
        /// <param name="connect"></param>
        /// <returns></returns>
        async Task SendUpdateChatRoom(string? url, string? userName, ChatInfo connect)
        {
            try
            {
                var r = _connections.GetContextIdForLogin(url, userName);
                if (r?.Count() > 0)
                {
                    await Clients.Clients(r).UpdateChatRoom(connect);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendUpdateChatRoom));
            }
        }

        /// <summary>
        /// Сохраняем новый чат для текущего подключения
        /// </summary>
        /// <param name="connect"></param>
        /// <returns></returns>
        public async Task SaveNewConnect(ChatInfo connect)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.UserName))
                {
                    List<Task> loadTask = new();

                    var request = new ChatAndUserKey()
                    {
                        UserKey = new() { AuthorityUrl = _getMyKeyConnect.AuthorityUrl, UserName = _getMyKeyConnect.UserName },
                        Chat = ToProtoModel(connect)
                    };
                    var b = await _pods.AddChatAsync(request);
                    if (b?.Value == true)
                    {
                        lock (AllChats)
                        {
                            if (!GetChatListForUser.Any(x => x.Key == connect.Key))
                            {
                                AllChats.First(x => x.UserName == _getMyKeyConnect.UserName && IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)).Chats.Add(connect);
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, connect));
                            }
                        }
                        await Task.WhenAll(loadTask);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SaveNewConnect));
            }
        }

        /// <summary>
        /// Изменяем название чата
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public async Task ChangeNameConnect(Guid keyChatRoom, string newName)
        {
            try
            {
                if (string.IsNullOrEmpty(_getMyKeyConnect?.UserName)) return;

                if (GetChatListForUser.Any(x => x.Key == keyChatRoom))
                {
                    var conn = GetChatListForUser.First(x => x.Key == keyChatRoom);
                    var request = new NewNameForChat()
                    {
                        Key = new()
                        {
                            UserKey = new() { AuthorityUrl = _getMyKeyConnect.AuthorityUrl, UserName = _getMyKeyConnect.UserName },
                            ChatKey = keyChatRoom.ToString(),
                        },
                        NewName = newName
                    };
                    var b = await _pods.UpdateNameChatAsync(request);
                    if (b.Value == true)
                    {
                        conn.NameRoom = newName;
                        await SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ChangeNameConnect));
            }
        }

        /// <summary>
        /// Удаляем чат для текущего подключения по ключу
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        public async Task DeleteChatRoom(Guid keyChatRoom)
        {
            try
            {
                if (string.IsNullOrEmpty(_getMyKeyConnect?.UserName) || string.IsNullOrEmpty(_getMyKeyConnect.AuthorityUrl)) return;


                var con = FindChatForKey(_getMyKeyConnect.UserName, _getMyKeyConnect.AuthorityUrl, keyChatRoom);

                List<Task> loadTask = new();
                if (con != null)
                {
                    var requestConnect = new JoinModel(new(con) { Items = new() { new(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName) } }, _getMyKeyConnect, null);
                    foreach (var connect in con.Items.Where(s => s.UserName != _getMyKeyConnect.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, s.AuthorityUrl)))
                    {
                        if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                        {
                            try
                            {
                                requestConnect.ForUrl = new(connect.AuthorityUrl, connect.UserName);

                                //для локальных пользователей

                                loadTask.Add(SendLocalOrRemotaRequest(SetDeleteItemsForChatRoom, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));
                            }
                            catch (Exception ex)
                            {
                                _logger.WriteLogError(ex, $"{nameof(DeleteChatRoom)} for {connect.AuthorityUrl}");
                            }
                        }
                    }

                }
                await DeleteChat(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom);

                await Task.WhenAll(loadTask);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(DeleteChatRoom));
            }
        }

        async Task SendLocalOrRemotaRequest<TRequest>(Func<TRequest, Task> action, TRequest data, string? myUrl, string? remoteUrl)
        {
            try
            {
                if (IsLocalAuthorityUrl(myUrl, remoteUrl))
                {
                    await action.Invoke(data);
                }
                else //для удаленных пользователей
                {
                    _ = SendJsonToRemoteClient(remoteUrl, action.GetMethodInfo().Name, data);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendLocalOrRemotaRequest));
            }
        }

        async Task DeleteChat(string forUrl, string forUser, Guid keyChatRoom)
        {
            try
            {
                await DeleteChatRoomForUser(forUrl, forUser, keyChatRoom);

                var b = await _pods.DeleteChatAsync(new UserKeyAndChatKey()
                {
                    UserKey = new() { AuthorityUrl = forUrl, UserName = forUser },
                    ChatKey = keyChatRoom.ToString(),
                });
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(DeleteChat));
            }
        }

        /// <summary>
        /// Закрываем все подключения и удаляем чат пользователя по ключу
        /// </summary>
        /// <param name="forUrl"></param>
        /// <param name="forUser"></param>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        async Task DeleteChatRoomForUser(string? forUrl, string? forUser, Guid keyChatRoom)
        {
            try
            {
                if (!string.IsNullOrEmpty(forUser))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatListForUser(forUrl, forUser)?.FirstOrDefault(x => x.Key == keyChatRoom);
                        if (conn != null)
                        {
                            if (GetStateCalling(conn) >= StateCall.Calling)
                            {
                                var urlCloseList = conn.Items.Where(x => x.State >= StateCall.Calling);
                                if (urlCloseList.Count() > 0)
                                {
                                    foreach (var con in urlCloseList)
                                    {
                                        if (!string.IsNullOrEmpty(con?.AuthorityUrl))
                                        {
                                            loadTask.Add(CloseP2P(keyChatRoom, forUser, con.AuthorityUrl, con.UserName));

                                            var requestConnect = new KeyChatForUrl() { KeyChatRoom = keyChatRoom, RemoteUrl = new(forUrl, forUser), ForUrl = new(con.AuthorityUrl, con.UserName) };

                                            loadTask.Add(SendLocalOrRemotaRequest(CloseCall, requestConnect, forUrl, con.AuthorityUrl));

                                        }
                                    }
                                }
                            }
                            AllChats.First(x => x.UserName == forUser && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl)).Chats.RemoveAll(x => x.Key == keyChatRoom);
                            loadTask.Add(DeleteChatForKey(forUrl, forUser, keyChatRoom));
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(DeleteChatRoomForUser));
            }
        }

        /// <summary>
        /// Отправляем пользователю входящий вызов
        /// </summary>
        /// <param name="forUrl"></param>
        /// <param name="forUser"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        async Task SetInCallingConnect(string? forUrl, string? forUser, ChatInfo? conn)
        {
            try
            {
                var r = _connections.GetContextIdForLogin(forUrl, forUser);
                if (r?.Count() > 0)
                {
                    await Clients.Clients(r).SetInCallingConnect(conn);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetInCallingConnect));
            }
        }

        /// <summary>
        /// Добавляем новых пользователей в чат для текущего подключения, отправляем пользователям уведомления о присоединении
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task SendAddItemsForChatRoom(Guid keyChatRoom, IEnumerable<ConnectInfo> items)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        if (GetChatListForUser.Any(x => x.Key == keyChatRoom))
                        {
                            var conn = GetChatListForUser.First(x => x.Key == keyChatRoom);
                            if (conn.UserCreate == _getMyKeyConnect.UserName && conn.Items.Any(x => x.UserName == _getMyKeyConnect.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, _getMyKeyConnect.AuthorityUrl)))
                            {
                                //Список новых пользователей
                                var addItems = items.ExceptBy(conn.Items.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), (x) => $"{x.AuthorityUrl}&{x.UserName}").ToList();

                                if (addItems.Count > 0)
                                {
                                    conn.Items.AddRange(addItems);

                                    loadTask.Add(SaveToBaseNewConnectList(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, addItems));

                                    //обнавляем чаты у других пользователей
                                    foreach (var connect in conn.Items.Where(x => x.UserName != _getMyKeyConnect.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)))
                                    {
                                        if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                        {
                                            try
                                            {
                                                var requestConnect = new JoinModel(new(conn), _getMyKeyConnect, new(connect.AuthorityUrl, connect.UserName));

                                                loadTask.Add(SendLocalOrRemotaRequest(SetAddItemsForChatRoom, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));

                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.WriteLogError(ex, $"{nameof(SendAddItemsForChatRoom)} to {nameof(SetAddItemsForChatRoom)} for {connect.AuthorityUrl}");
                                            }
                                        }
                                    }
                                    //если уже идет вызов, вызываем новых пользователей
                                    if (GetStateCalling(conn) >= StateCall.Calling)
                                    {
                                        //Проходим по новым пользователям
                                        foreach (var connect in conn.Items.Where(x => addItems.Any(a => a.UserName == x.UserName && IpAddressUtilities.CompareForAuthority(a.AuthorityUrl, x.AuthorityUrl))))
                                        {
                                            if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                            {
                                                try
                                                {
                                                    connect.State = StateCall.Calling;
                                                    loadTask.Add(LoadAnswerForUrl(keyChatRoom, _getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, connect.AuthorityUrl, connect.UserName, StateCall.Calling));

                                                    var requestConnect = new JoinModel(new(conn), _getMyKeyConnect, new(connect.AuthorityUrl, connect.UserName));

                                                    loadTask.Add(SendLocalOrRemotaRequest(JoinToChatRoom, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));
                                                }
                                                catch (Exception ex)
                                                {
                                                    connect.State = StateCall.Disconnect;
                                                    _logger.WriteLogError(ex, $"{nameof(SendAddItemsForChatRoom)} to {nameof(JoinToChatRoom)} for {connect.AuthorityUrl}");
                                                }
                                            }
                                        }
                                    }
                                    loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                                }
                            }
                        }
                    }

                    await Task.WhenAll(loadTask);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendAddItemsForChatRoom));
            }
        }

        /// <summary>
        /// Добавляем список подключений для чата в базе
        /// </summary>
        /// <param name="forUrl"></param>
        /// <param name="forUser"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        async Task SaveToBaseNewConnectList(string? forUrl, string? forUser, Guid? keyChatRoom, IEnumerable<ConnectInfo> itemsForAdd)
        {
            try
            {
                var request = new ConnectList() { Key = new() { UserKey = new() { AuthorityUrl = forUrl, UserName = forUser }, ChatKey = keyChatRoom.ToString() } };
                request.Items.AddRange(itemsForAdd.Select(x => new PodsProto.V1.ConnectInfo() { AuthorityUrl = x.AuthorityUrl, UserName = x.UserName, State = PodsProto.V1.StateCall.Disconnect }));
                await _pods.AddConnectsInChatAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SaveToBaseNewConnectList));
            }
        }

        /// <summary>
        /// Удаляем подключения для чата в базе
        /// </summary>
        /// <param name="forUrl"></param>
        /// <param name="forUser"></param>
        /// <param name="itemsForDelete"></param>
        /// <returns></returns>
        async Task DeleteToBaseConnectListForChat(string? forUrl, string? forUser, Guid? keyChatRoom, IEnumerable<ConnectInfo> itemsForDelete)
        {
            try
            {
                var request = new ConnectList() { Key = new() { UserKey = new() { AuthorityUrl = forUrl, UserName = forUser }, ChatKey = keyChatRoom.ToString() } };
                request.Items.AddRange(itemsForDelete.Select(x => new PodsProto.V1.ConnectInfo() { AuthorityUrl = x.AuthorityUrl, UserName = x.UserName, State = PodsProto.V1.StateCall.Disconnect }));
                await _pods.DeleteConnectsInChatAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(DeleteToBaseConnectListForChat));
            }
        }

        /// <summary>
        /// Удаляем пользователей из чат для текущего подключения, отправляем пользователям уведомления о удалении
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task SendDeleteItemsForChatRoom(Guid keyChatRoom, IEnumerable<ConnectInfo> items)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        if (GetChatListForUser.Any(x => x.Key == keyChatRoom))
                        {
                            var conn = GetChatListForUser.First(x => x.Key == keyChatRoom);
                            if (conn.UserCreate == _getMyKeyConnect.UserName && conn.Items.Any(x => x.UserName == _getMyKeyConnect.UserName && IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)))
                            {
                                var urlList = conn.Items.Where(x => _getMyKeyConnect.UserName != x.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)).ToList();

                                conn.Items.RemoveAll(x => items.Any(d => d.UserName == x.UserName && IpAddressUtilities.CompareForAuthority(d.AuthorityUrl, x.AuthorityUrl)));
                                //Удаляем из базы
                                loadTask.Add(DeleteToBaseConnectListForChat(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, items));
                                foreach (var connect in urlList)
                                {
                                    if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                    {
                                        try
                                        {
                                            var requestConnect = new JoinModel(new(conn), _getMyKeyConnect, new(connect.AuthorityUrl, connect.UserName));

                                            loadTask.Add(SendLocalOrRemotaRequest(SetDeleteItemsForChatRoom, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.WriteLogError(ex, $"{nameof(SendDeleteItemsForChatRoom)} for {connect.AuthorityUrl}");
                                        }
                                    }
                                }
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                            }
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendDeleteItemsForChatRoom));
            }
        }

        /// <summary>
        /// Приходит уведомление о добавлении новых пользователей в чат
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task SetAddItemsForChatRoom(JoinModel model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.Connect.Key);

                        if (conn != null)
                        {
                            if (conn.UserCreate == model.RemoteUrl.UserName)
                            {
                                //Список новых пользователей для чата
                                var addItems = model.Connect.Items.ExceptBy(conn.Items.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), (x) => $"{x.AuthorityUrl}&{x.UserName}").ToList();
                                if (addItems.Count > 0)
                                {
                                    conn.Items.AddRange(addItems);

                                    //Сохраняем в базе
                                    loadTask.Add(SaveToBaseNewConnectList(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn.Key, addItems));

                                    //Если текущая комната активна, запускаем подключение к новым пользователям
                                    if (GetStateCalling(conn) >= StateCall.Calling)
                                    {
                                        foreach (var connect in conn.Items.Where(x => x.State < StateCall.Calling && addItems.Any(a => x.UserName == a.UserName && IpAddressUtilities.CompareForAuthority(a.AuthorityUrl, x.AuthorityUrl))))
                                        {
                                            if (!string.IsNullOrEmpty(connect.AuthorityUrl))
                                            {
                                                connect.State = StateCall.Calling;
                                                loadTask.Add(LoadAnswerForUrl(model.Connect.Key, model.ForUrl.AuthorityUrl, model.ForUrl.UserName, connect.AuthorityUrl, connect.UserName, StateCall.Calling));
                                            }
                                        }
                                    }
                                    loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                                }
                            }
                        }
                        else if (model.Connect.Items.Count > 1 && model.Connect.Items.Any(x => x.UserName == model.ForUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.ForUrl.AuthorityUrl)))
                        {
                            loadTask.Add(CreateChatRoom(model));
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetAddItemsForChatRoom));
            }
        }

        /// <summary>
        /// Приходит уведомление о удалении пользователей из чата
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task SetDeleteItemsForChatRoom(JoinModel model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.ForUrl.AuthorityUrl) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.Connect.Key);

                        if (conn != null)
                        {
                            List<ConnectInfo> deleteItems = new();
                            if (model.Connect.Items.Count == 1 && model.Connect.Items[0].UserName == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(model.Connect.Items[0].AuthorityUrl, model.RemoteUrl.AuthorityUrl))
                            {
                                deleteItems = model.Connect.Items;
                            }
                            else if (conn.UserCreate == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(conn.AuthorityCreate, model.RemoteUrl.AuthorityUrl))
                            {
                                deleteItems = conn.Items.ExceptBy(model.Connect.Items.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), (x) => $"{x.AuthorityUrl}&{x.UserName}").ToList();
                            }


                            if (deleteItems.Count > 0)
                            {
                                foreach (var forUrl in deleteItems.Where(x => x.State >= StateCall.Calling))
                                {
                                    if (!string.IsNullOrEmpty(forUrl?.AuthorityUrl))
                                    {
                                        //Закрываем подключение
                                        loadTask.Add(CloseP2P(model.Connect.Key, model.ForUrl.UserName, forUrl.AuthorityUrl, forUrl.UserName));
                                    }
                                }

                                //если мы являемся объектом удаления, закрываем все подключения в чате, и удаляемся
                                if (deleteItems.Any(x => x.UserName == model.ForUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.ForUrl.AuthorityUrl)))
                                {
                                    loadTask.Add(DeleteChat(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.Connect.Key));
                                }
                                else
                                {
                                    conn.Items.RemoveAll(x => deleteItems.Any(d => d.UserName == x.UserName && IpAddressUtilities.CompareForAuthority(d.AuthorityUrl, x.AuthorityUrl)));
                                    loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                                    //Удаляем из базы
                                    loadTask.Add(DeleteToBaseConnectListForChat(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn.Key, deleteItems));
                                }
                            }
                        }
                        else if (model.Connect.Items.Count > 1 && model.Connect.Items.Any(x => x.UserName == model.ForUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.ForUrl.AuthorityUrl)))
                        {
                            loadTask.Add(CreateChatRoom(model));
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetDeleteItemsForChatRoom));
            }
        }

        bool IsLocalAuthorityUrl(string? myUrl, string? remoteUrl)
        {
            if (string.IsNullOrWhiteSpace(remoteUrl)) return false;
            return (IpAddressUtilities.CompareForHost(remoteUrl, "localhost") || IpAddressUtilities.CompareForHost(remoteUrl, "127.0.0.1") || IpAddressUtilities.CompareForHost(remoteUrl, "0.0.0.0") || IpAddressUtilities.CompareForAuthority(remoteUrl, myUrl));
        }

        /// <summary>
        /// Отправляем данные на удаленный сервер
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="forUrl">адрес скрвера</param>
        /// <param name="methodName">наименование метода</param>
        /// <param name="model">данные</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpStatusCode> SendJsonToRemoteClient<TData>(string? forUrl, string methodName, TData model, CancellationToken cancellationToken = default)
        {
            return SendRemoteClient(forUrl, methodName, JsonContent.Create(model), cancellationToken);
        }

        async Task<HttpStatusCode> SendRemoteClient(string? forUrl, string methodName, HttpContent model, CancellationToken cancellationToken = default)
        {
            try
            {
                forUrl = IpAddressUtilities.GetAuthority(forUrl);
                if (!string.IsNullOrEmpty(forUrl) && !IsLocalAuthorityUrl(_getMyKeyConnect?.AuthorityUrl, forUrl))
                {
                    var absoluteUri = $"https://{forUrl}";
                    try
                    {
                        using var result = await _httpClient.PostAsync(absoluteUri, $"api/v1/chat/{methodName}", model, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                        return result.StatusCode;
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLogError(ex, $"{nameof(SendRemoteClient)} for {forUrl}, methodName {methodName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(SendRemoteClient)} for {forUrl}");
            }
            return HttpStatusCode.BadRequest;
        }

        public async Task SendAllUsersFile(Guid keyChatRoom, string Message, string fullPath)
        {
            try
            {
                var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                if (conn != null && !string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    var message = new ChatMessage(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, Message, fullPath);

                    await AddMessageForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, message);

                    fullPath = Path.Combine(DirectoryTmp, fullPath);

                    var items = conn.Items.Where(x => x.UserName != _getMyKeyConnect?.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect?.AuthorityUrl, x.AuthorityUrl)).GroupBy(x => x.AuthorityUrl).ToList();

                    var myUrls = items.Where(x => IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.Key)).ToList().SelectMany(x => x);

                    var otherUrl = items.Where(x => !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.Key)).ToList();

                    ReplicateFilesRequest request = new()
                    {
                        RemoteUrl = _getMyKeyConnect,
                        Message = Message,
                        FileUrl = message.Url,
                        KeyChatRoom = keyChatRoom
                    };

                    foreach (var forUrl in otherUrl)
                    {
                        try
                        {
                            request.UserNames = forUrl.Select(x => x.UserName ?? string.Empty).ToArray();
                            _ = SendJsonToRemoteClient(forUrl.Key, nameof(ReplicateFiles), request);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(@"Ошибка отправки файла на удаленный адрес {Ip}: {Message}", forUrl.Key, ex.Message);
                        }
                    }
                    await ReplicateFiles(myUrls.Select(x => x.UserName), keyChatRoom, message.AuthorityUrl, message);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendAllUsersFile));
            }
        }

        public async Task ReplicateFiles(IEnumerable<string?> users, Guid keyChatRoom, string authorityUrl, ChatMessage message)
        {
            try
            {
                var writePath = Path.Combine(DirectoryTmp, message.Url);
                foreach (var user in users)
                {
                    var filePath = CreatePathForChat(keyChatRoom, user);

                    filePath = Path.Combine(filePath, Path.ChangeExtension(Path.GetRandomFileName(), Path.GetExtension(writePath)));

                    var copyPath = Path.Combine(DirectoryTmp, filePath);

                    var dir = Path.GetDirectoryName(copyPath);

                    if (!string.IsNullOrEmpty(dir))
                    {
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        if (!System.IO.File.Exists(copyPath))
                        {
                            System.IO.File.Copy(writePath, copyPath);

                            message.Url = filePath;

                            await AddMessageForUser(authorityUrl, user, keyChatRoom, message);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ReplicateFiles));
            }
        }

        /// <summary>
        /// Локальный вызов, исходящий вызов, устанавливаем StateCall.Calling
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="typeConnect"></param>
        /// <returns></returns>
        public async Task StartChatRoom(Guid keyChatRoom, TypeConnect typeConnect)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();

                    ///проверка на потерю связи
                    var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                    if (conn != null && GetStateCalling(conn) >= StateCall.Calling)
                    {
                        bool isClose = false;
                        if (conn.IdUiConnect != null)
                        {
                            var myGuid = _connections.GetGuidForContextId(Context.ConnectionId);

                            var contextId = _connections.GetContextIdForGuid(conn.IdUiConnect);

                            //разные приложения
                            if (conn.IdUiConnect != myGuid)
                            {
                                if (!contextId?.Any() ?? true)
                                {
                                    isClose = true;
                                }
                            }
                            else
                            {
                                isClose = true;
                            }
                        }
                        else
                        {
                            isClose = true;
                        }

                        if (isClose)
                        {
                            SetAllConnItemsState(conn, StateCall.Error);
                        }
                    }
                    lock (AllChats)
                    {
                        _logger.LogTrace(@"Исходящий вызов к {NameRoom}, инициатор {UserName}", conn?.NameRoom, _getMyKeyConnect.UserName);

                        if (conn != null)
                        {
                            loadTask.Add(AddMessageForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, new(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, $"{(typeConnect == TypeConnect.Video ? DispRep["OUT_VIDEO_CALL"] : DispRep["OUT_CALL"])}")));

                            if (GetStateCalling(conn) <= StateCall.Disconnect)
                            {
                                var sendToUrl = conn.Items.Where(x => x.UserName != _getMyKeyConnect.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl));

                                conn.IdUiConnect = _connections.GetGuidForContextId(Context.ConnectionId);

                                conn.OutTypeConn = typeConnect;

                                conn.Items.ForEach(x => x.State = StateCall.Disconnect);

                                if (sendToUrl?.Count() > 0)
                                {
                                    foreach (var connect in sendToUrl)
                                    {
                                        if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                        {
                                            try
                                            {
                                                connect.State = StateCall.Calling;
                                                //Запускаем ожидание для адреса
                                                loadTask.Add(LoadAnswerForUrl(keyChatRoom, _getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, connect.AuthorityUrl, connect.UserName, StateCall.Calling));

                                                var requestConnect = new JoinModel() { Connect = new(conn), RemoteUrl = _getMyKeyConnect, ForUrl = new(connect.AuthorityUrl, connect.UserName) };

                                                loadTask.Add(SendLocalOrRemotaRequest(JoinToChatRoom, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));

                                            }
                                            catch (Exception ex)
                                            {
                                                connect.State = StateCall.Error;
                                                _logger.WriteLogError(ex, $"{nameof(StartChatRoom)} for {connect.AuthorityUrl}");
                                            }
                                        }
                                    }
                                }
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                                //Запускаем ожидание для комнаты
                                loadTask.Add(LoadChangeStateChat(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom));
                            }
                            else
                            {
                                loadTask.Add(Clients.Clients(Context.ConnectionId).AddMessageForChat(keyChatRoom, new ChatMessage(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, DispRep["ACTIVE_OTHER_DEVICE"])));
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(StartChatRoom));
            }
        }

        /// <summary>
        /// Удаленный вызов, запрос на подключение
        /// </summary>
        /// <param name="inUrlCalling"></param>
        /// <param name="newConnect"></param>
        /// <returns></returns>
        public async Task JoinToChatRoom(JoinModel model)
        {
            TypeAnswerForJoin response = TypeAnswerForJoin.Error;
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        _logger.LogTrace(@"Запрос на создание комнаты {NameRoom}, от {RemoteUrl}, для {UserName}", model.Connect.NameRoom, model.RemoteUrl.UserName, model.ForUrl.UserName);
                        if (model.Connect.Key != Guid.Empty && model.Connect.Items.Count > 0)
                        {
                            model.Connect.IdUiConnect = null;

                            CreateChatRoom(model).Wait();

                            var chatsForUser = FindChatListForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName);
                            var findChats = chatsForUser.FirstOrDefault(x => x.Key == model.Connect.Key);

                            if (findChats != null)
                            {
                                if (chatsForUser.Any(x => x.Items.Any(i => (int)i.State >= (int)StateCall.Calling)))
                                {
                                    //если у нас активна текущая комната, необходимо присоединить абонента к комнате
                                    if (chatsForUser.Any(x => x.Key == findChats.Key && x.Items.Any(i => (int)i.State >= (int)StateCall.Calling)))
                                    {
                                        var con = findChats.Items.FirstOrDefault(x => x.UserName == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));
                                        if (con != null)
                                        {
                                            if (con.State >= StateCall.Calling)
                                            {
                                                response = TypeAnswerForJoin.LostConnect;
                                            }
                                            else
                                            {
                                                con.State = StateCall.Calling;
                                                //запускаем ожидание подключения пользователя
                                                loadTask.Add(LoadAnswerForUrl(findChats.Key, model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, StateCall.Calling));
                                                response = TypeAnswerForJoin.Active;
                                            }
                                        }
                                    }
                                    else //другое активное подключение
                                    {
                                        response = TypeAnswerForJoin.Busy;
                                        loadTask.Add(AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, findChats.Key, new(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, DispRep["MISSED_CALL"])));
                                    }
                                }
                                else
                                {
                                    if (MissedCall.ContainsKey(model.ForUrl.UserName))
                                        MissedCall.Remove(model.ForUrl.UserName);
                                    //записываем в пропущенный вызов на случай если пользователь не в сети
                                    MissedCall.Add(model.ForUrl.UserName, findChats);

                                    findChats.Items.ForEach(x => x.State = StateCall.Disconnect);
                                    findChats.IdUiConnect = null;
                                    loadTask.Add(SetInCallingConnect(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, findChats));

                                    //запускаем ожидание на ответ для входящего вызова
                                    loadTask.Add(LoadAnswerCall(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, findChats.Key));

                                    loadTask.Add(AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, findChats.Key, new(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, DispRep["IN_CALL"])));

                                    PushMessageChat pushMessage = new();
                                    pushMessage.Url = $"/";
                                    pushMessage.Title = findChats.NameRoom;
                                    pushMessage.KeyChatRoom = findChats.Key.ToString();
                                    pushMessage.ForUser = model.ForUrl?.UserName;
                                    pushMessage.ForUrl = model.ForUrl?.AuthorityUrl;
                                    pushMessage.Message = $"{DispRep["IN_CALL"]} от {GetCuName(model.RemoteUrl?.AuthorityUrl)} - {model.RemoteUrl?.UserName}";

                                    loadTask.Add(SendPushAsync(pushMessage));

                                    response = TypeAnswerForJoin.Ready;
                                }

                                var requestConnect = new AnswerForJoin() { KeyChatRoom = findChats.Key, RemoteUrl = model.ForUrl, ForUrl = model.RemoteUrl, TypeAnswer = response };

                                loadTask.Add(SendLocalOrRemotaRequest(AnswerForJoinChatRoom, requestConnect, model.ForUrl?.AuthorityUrl, model.RemoteUrl?.AuthorityUrl));

                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(JoinToChatRoom));
            }
        }

        /// <summary>
        /// Приходит предварительный ответ на исходящий вызов, обновляем статусы
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task AnswerForJoinChatRoom(AnswerForJoin model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl.AuthorityUrl))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                        if (conn != null)
                        {
                            if (GetStateCalling(conn) >= StateCall.Calling)
                            {
                                var con = conn.Items.FirstOrDefault(x => x.UserName == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(model.RemoteUrl.AuthorityUrl, x.AuthorityUrl));

                                if (con != null && con.State == StateCall.Calling)
                                {
                                    if (model.TypeAnswer == TypeAnswerForJoin.LostConnect || model.TypeAnswer == TypeAnswerForJoin.Active)
                                    {
                                        con.State = StateCall.CreateAnswer;
                                        loadTask.Add(LoadAnswerForUrl(model.KeyChatRoom, model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, StateCall.CreateAnswer));
                                        loadTask.Add(Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).GoConnectionP2P(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.KeyChatRoom));
                                    }
                                    else if (model.TypeAnswer == TypeAnswerForJoin.Error)
                                    {
                                        con.State = StateCall.Error;
                                    }
                                    else if (model.TypeAnswer == TypeAnswerForJoin.Busy)
                                    {
                                        loadTask.Add(AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.KeyChatRoom, new ChatMessage(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, DispRep["BUSY_CALL"])));
                                        con.State = StateCall.Aborted;
                                    }
                                }
                            }
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(AnswerForJoinChatRoom));
            }
        }

        /// <summary>
        /// Локальный вызов, отвечаем на вызов, мы готовы инициализировать P2P соединение, устанавливаем StateCall.Calling
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="typeConnect"></param>
        /// <returns></returns>
        public async Task ReadyCreateP2P(Guid keyChatRoom, TypeConnect typeConnect)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        if (MissedCall != null && MissedCall.ContainsKey(_getMyKeyConnect.UserName) && MissedCall[_getMyKeyConnect.UserName].Key == keyChatRoom)
                        {
                            MissedCall.Remove(_getMyKeyConnect.UserName);
                        }

                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                        _logger.LogTrace(@"{UserName} принимает входящий вызов к комнате {NameRoom}", _getMyKeyConnect.UserName, conn?.NameRoom);
                        if (conn != null)
                        {
                            //если данная комната не активна на другом устройстве
                            if (GetStateCalling(conn) <= StateCall.Disconnect)
                            {
                                //удаляем индикацию входящего вызова
                                loadTask.Add(SetInCallingConnect(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, null));

                                //устанавливаем id пользовательского интерфейса инициировавшего ответ
                                conn.IdUiConnect = _connections.GetGuidForContextId(Context.ConnectionId);

                                conn.OutTypeConn = typeConnect;
                                //обнуляем состояние подключений
                                conn.Items.ForEach(x =>
                                {
                                    x.State = StateCall.Disconnect;
                                });

                                foreach (var connect in conn.Items.Where(x => x.UserName != _getMyKeyConnect.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect?.AuthorityUrl, x.AuthorityUrl)))
                                {
                                    if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                    {
                                        try
                                        {
                                            connect.State = StateCall.Calling;
                                            loadTask.Add(LoadAnswerForUrl(keyChatRoom, _getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, connect.AuthorityUrl, connect.UserName, StateCall.Calling));

                                            var requestConnect = new KeyChatForUrl { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(connect.AuthorityUrl, connect.UserName) };

                                            //отправляем согласие на Р2Р
                                            loadTask.Add(SendLocalOrRemotaRequest(GoConnectionP2P, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));
                                        }
                                        catch (Exception ex)
                                        {
                                            connect.State = StateCall.Disconnect;
                                            _logger.LogError(ex.Message);
                                        }
                                    }
                                }
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                                loadTask.Add(LoadChangeStateChat(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom));
                            }
                        }
                    }

                    await Task.WhenAll(loadTask);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ReadyCreateP2P));
            }
        }

        /// <summary>
        /// Удаленный вызов, приходит согласие на установку P2P соединения, устанавливаем StateCall.Calling
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        public async Task GoConnectionP2P(KeyChatForUrl model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl.AuthorityUrl))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);

                        _logger.LogTrace(@"Пришло согласие на установку P2P к комнате {NameRoom}, от {RemoteUrl} для {UserName}", conn?.NameRoom, model.RemoteUrl.AuthorityUrl, model.ForUrl.UserName);

                        //общее состояние подключения для P2P не может быть Disconnect
                        if (conn != null)
                        {
                            if (conn.IdUiConnect != null && _connections.AnyGuid(conn.IdUiConnect) && GetStateCalling(conn) > StateCall.Disconnect)
                            {
                                var con = conn.Items.FirstOrDefault(x => x.UserName == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));

                                if (con != null && con.State == StateCall.Calling)
                                {
                                    con.State = StateCall.CreateAnswer;
                                    loadTask.Add(LoadAnswerForUrl(model.KeyChatRoom, model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, StateCall.CreateAnswer, 10, async (keyRoom, myUser, forUrl, forUser) => await ErrorConnectP2P(keyRoom, myUser, forUrl, forUser)));
                                    loadTask.Add(Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).GoConnectionP2P(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.KeyChatRoom));
                                }
                                else if (con?.State >= StateCall.CreateP2P)//уже есть подключение с другим устройствам
                                {
                                    SetAllConnItemsState(conn, StateCall.Disconnect);
                                }
                            }
                            loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                        }
                    }
                    await Task.WhenAll(loadTask);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(GoConnectionP2P));
            }
        }

        /// <summary>
        /// Локальный вызов, отправляем наш offer, устанавливаем StateCall.CreateP2P
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="forUrl"></param>
        /// <param name="offer"></param>
        /// <returns></returns>
        public async Task SendOfferForClient(string forUrl, string forUserName, Guid keyChatRoom, string offer)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        forUrl = IpAddressUtilities.GetAuthority(forUrl);
                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                        _logger.LogTrace(@"Отправляем наш Offer к комнате {NameRoom}, для {forUrl} от {UserName}", conn?.NameRoom, forUrl, _getMyKeyConnect.UserName);
                        if (conn != null && GetStateCalling(conn) >= StateCall.CreateAnswer)
                        {
                            var con = conn.Items.FirstOrDefault(x => x.UserName == forUserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                            if (con != null && con.State == StateCall.CreateAnswer)
                            {
                                if (string.IsNullOrEmpty(offer))
                                {
                                    con.State = StateCall.Error;
                                }
                                else
                                {
                                    con.State = StateCall.CreateP2P;
                                    loadTask.Add(LoadAnswerForUrl(keyChatRoom, _getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, forUrl, forUserName, StateCall.CreateP2P, 10, async (keyRoom, myUser, forUrl, forUser) => await ErrorConnectP2P(keyRoom, myUser, forUrl, forUser)));

                                    var requestConnect = new KeyChatForUrlAndValue() { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(con.AuthorityUrl, con.UserName), Value = offer };

                                    loadTask.Add(SendLocalOrRemotaRequest(SetRemoteOfferForUrl, requestConnect, _getMyKeyConnect.AuthorityUrl, con.AuthorityUrl));
                                }
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendOfferForClient));
            }
        }

        /// <summary>
        /// Удаленный вызов, приходи offer, устанавливаем StateCall.CreateAnswer
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="offer"></param>
        /// <returns></returns>
        public async Task SetRemoteOfferForUrl(KeyChatForUrlAndValue model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl.AuthorityUrl))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);

                        _logger.LogTrace(@"Пришел Offer к комнате {NameRoom}, от {forUrl} для {UserName}", conn?.NameRoom, model.RemoteUrl.AuthorityUrl, model.ForUrl.UserName);
                        if (conn != null)
                        {
                            if (conn.IdUiConnect != null && _connections.AnyGuid(conn.IdUiConnect) && GetStateCalling(conn) >= StateCall.Calling)
                            {
                                var con = conn.Items.FirstOrDefault(x => x.UserName == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));
                                if (con != null)
                                {
                                    if (con.State == StateCall.Calling)
                                    {
                                        con.State = StateCall.CreateAnswer;
                                        loadTask.Add(LoadAnswerForUrl(model.KeyChatRoom, model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, StateCall.CreateAnswer, 10, async (keyRoom, myUser, forUrl, forUser) => await ErrorConnectP2P(keyRoom, myUser, forUrl, forUser)));
                                        loadTask.Add(Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).SetRemoteOfferForUrl(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.KeyChatRoom, model.Value));
                                    }
                                    else if (con.State == StateCall.Connected)
                                    {
                                        con.State = StateCall.ChangeStream;
                                        loadTask.Add(LoadAnswerForUrl(model.KeyChatRoom, model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, StateCall.ChangeStream, 10, async (keyRoom, myUser, forUrl, forUser) => await ErrorConnectP2P(keyRoom, myUser, forUrl, forUser)));
                                        loadTask.Add(Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).SetChangeRemoteOfferForUrl(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.KeyChatRoom, model.Value));
                                    }
                                }
                            }
                            loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetRemoteOfferForUrl));
            }
        }

        /// <summary>
        /// Локальный вызов, сформировали ответ на offer, устанавливаем StateCall.CreateP2P
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="forUrl"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public async Task SendAnswerForRemoteClient(string forUrl, string forUserName, Guid keyChatRoom, string answer)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        forUrl = IpAddressUtilities.GetAuthority(forUrl);
                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                        _logger.LogTrace(@"Сформирован ответ на Offer к комнате {NameRoom}, для {forUrl} от {UserName}", conn?.NameRoom, forUrl, _getMyKeyConnect.UserName);
                        if (conn != null && GetStateCalling(conn) >= StateCall.Calling)
                        {
                            var con = conn.Items.FirstOrDefault(x => x.UserName == forUserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                            if (con != null && (con.State == StateCall.CreateAnswer || con.State == StateCall.ChangeStream))
                            {
                                if (string.IsNullOrEmpty(answer))
                                {
                                    con.State = StateCall.Error;
                                }
                                else
                                {
                                    if (con.State == StateCall.ChangeStream)
                                    {
                                        con.State = StateCall.Connected;
                                    }
                                    else
                                    {
                                        con.State = StateCall.CreateP2P;
                                        loadTask.Add(LoadAnswerForUrl(keyChatRoom, _getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, forUrl, forUserName, StateCall.CreateP2P, 10, async (keyRoom, myUser, forUrl, forUser) => await ErrorConnectP2P(keyRoom, myUser, forUrl, forUser)));
                                    }

                                    var requestConnect = new KeyChatForUrlAndValue() { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(con.AuthorityUrl, con.UserName), Value = answer };

                                    loadTask.Add(SendLocalOrRemotaRequest(SetAnswerForRemoteClient, requestConnect, _getMyKeyConnect.AuthorityUrl, con.AuthorityUrl));
                                }
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendAnswerForRemoteClient));
            }
        }

        /// <summary>
        /// Удаленный вызов, записываем ответ, устанавливаем StateCall.Connected если StateCall.ChangeStream
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public async Task SetAnswerForRemoteClient(KeyChatForUrlAndValue model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl.AuthorityUrl))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                        _logger.LogTrace(@"Пришел ответ на Offer к комнате {NameRoom}, от {forUrl} для {UserName}", conn?.NameRoom, model.RemoteUrl.AuthorityUrl, model.ForUrl.UserName);
                        if (conn != null && conn.IdUiConnect != null && _connections.AnyGuid(conn.IdUiConnect) && GetStateCalling(conn) >= StateCall.CreateP2P)
                        {
                            var con = conn.Items.FirstOrDefault(x => x.UserName == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));

                            if (con != null && (con.State == StateCall.CreateP2P || con.State == StateCall.ChangeStream))
                            {
                                loadTask.Add(Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).SetRemoteAnswerForUrl(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.KeyChatRoom, model.Value));
                                if (con.State == StateCall.ChangeStream)
                                {
                                    con.State = StateCall.Connected;
                                    loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                                }
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetAnswerForRemoteClient));
            }
        }


        /// <summary>
        /// Локальный вызов. Запускаем изменение Р2Р подключения
        /// </summary>
        /// <param name="forUrl"></param>
        /// <param name="forUserName"></param>
        /// <param name="keyChatRoom"></param>
        /// <param name="newOffer"></param>
        /// <param name="typeOutCall"></param>
        /// <returns></returns>
        public async Task SendChangeRemoteStream(string forUrl, string forUserName, Guid keyChatRoom, string newOffer, TypeConnect typeOutCall)
        {
            try
            {
                if (string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) || string.IsNullOrEmpty(_getMyKeyConnect.UserName)) return;

                List<Task> loadTask = new();

                forUrl = IpAddressUtilities.GetAuthority(forUrl);
                var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                _logger.LogTrace(@"{MyUser} отправляем новый Offer к комнате {NameRoom}, для {forUrl} пользователь {UserName}", _getMyKeyConnect.UserName, conn?.NameRoom, forUrl, forUserName);
                if (conn != null && GetStateCalling(conn) >= StateCall.Connected)
                {
                    conn.OutTypeConn = typeOutCall;
                    var con = conn.Items.FirstOrDefault(x => x.UserName == forUserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                    if (con != null && con.State == StateCall.Connected)
                    {
                        con.State = StateCall.ChangeStream;
                        loadTask.Add(LoadAnswerForUrl(keyChatRoom, _getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, forUrl, forUserName, StateCall.ChangeStream, 10, async (keyRoom, myUser, forUrl, forUser) => await ErrorConnectP2P(keyRoom, myUser, forUrl, forUser)));

                        var requestConnect = new KeyChatForUrlAndValue() { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(con.AuthorityUrl, con.UserName), Value = newOffer };

                        loadTask.Add(SendLocalOrRemotaRequest(SetChangeRemoteStream, requestConnect, _getMyKeyConnect.AuthorityUrl, forUrl));

                        loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                    }
                }

                await Task.WhenAll(loadTask);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendChangeRemoteStream));
            }
        }

        /// <summary>
        /// Удаленный вызов. Приходит уведомление о изменении Р2Р подключения
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task SetChangeRemoteStream(KeyChatForUrlAndValue model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl.AuthorityUrl))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                        _logger.LogTrace(@"{MyUser} Пришел новый Offer к комнате {NameRoom}, от {forUrl} {UserName}", model.ForUrl.UserName, conn?.NameRoom, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName);
                        if (conn != null)
                        {
                            if (conn.IdUiConnect != null && _connections.AnyGuid(conn.IdUiConnect) && GetStateCalling(conn) >= StateCall.Connected)
                            {
                                var con = conn.Items.FirstOrDefault(x => model.RemoteUrl.UserName == x.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));
                                if (con != null && con.State == StateCall.Connected)
                                {
                                    con.State = StateCall.ChangeStream;
                                    loadTask.Add(LoadAnswerForUrl(model.KeyChatRoom, model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, StateCall.ChangeStream, 10, async (keyRoom, myUser, forUrl, forUser) => await ErrorConnectP2P(keyRoom, myUser, forUrl, forUser)));
                                    loadTask.Add(Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).SetChangeRemoteOfferForUrl(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.KeyChatRoom, model.Value));
                                }
                            }
                            loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetChangeRemoteStream));
            }
        }


        /// <summary>
        /// Локальный вызов, обмен candidate
        /// </summary>
        /// <param name="forUrl"></param>
        /// <param name="forUserName"></param>
        /// <param name="keyChatRoom"></param>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public async Task SendCandidateForUrl(string forUrl, string forUserName, Guid keyChatRoom, string candidate)
        {
            try
            {
                if (string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) || string.IsNullOrEmpty(_getMyKeyConnect.UserName)) return;

                forUrl = IpAddressUtilities.GetAuthority(forUrl);
                var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                if (conn != null && GetStateCalling(conn) >= StateCall.Calling)
                {
                    var con = conn.Items.FirstOrDefault(x => x.UserName == forUserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                    if (con != null && con.State >= StateCall.Calling)
                    {
                        if (string.IsNullOrEmpty(candidate))
                        {
                            con.State = StateCall.Error;
                            await SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn);
                        }
                        else
                        {
                            var requestConnect = new KeyChatForUrlAndValue() { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(con.AuthorityUrl, con.UserName), Value = candidate };

                            await SendLocalOrRemotaRequest(SendCandidate, requestConnect, _getMyKeyConnect.AuthorityUrl, con.AuthorityUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendCandidateForUrl));
            }
        }

        /// <summary>
        /// Удаленный вызов, получен candidate
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task SendCandidate(KeyChatForUrlAndValue model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.ForUrl?.UserName) || string.IsNullOrEmpty(model.RemoteUrl?.UserName)) return;

                var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                if (conn != null && conn.IdUiConnect != null && _connections.AnyGuid(conn.IdUiConnect) && GetStateCalling(conn) >= StateCall.Calling)
                {
                    var con = conn.Items.FirstOrDefault(x => x.UserName == model.RemoteUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));

                    if (con != null && con.State >= StateCall.Calling)
                    {
                        await Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).SendCandidate(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.KeyChatRoom, model.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendCandidate));
            }
        }

        /// <summary>
        /// Локальный вызов, оборвана связь, устанавливаем StateCall.Disconnect
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="forUrl"></param>
        /// <returns></returns>
        public async Task DisconnectP2P(string forUrl, string forUserName, Guid keyChatRoom)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);

                        _logger.LogTrace(@"{MyName} соединение потеряно с {forUrl} пользоветель {UserName} к комнате {NameRoom}, состояние комнаты {State}", _getMyKeyConnect.UserName, forUrl, forUserName, conn?.NameRoom, GetStateCalling(conn));

                        if (conn != null && GetStateCalling(conn) >= StateCall.CreateP2P)
                        {
                            var con = conn.Items.FirstOrDefault(x => x.UserName == forUserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                            if (con != null && con.State >= StateCall.CreateP2P)
                            {
                                con.State = StateCall.Disconnect;
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(DisconnectP2P));
            }
        }

        /// <summary>
        /// Локальный вызов, связь установлена, устанавливаем StateCall.Connected
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="forUrl"></param>
        /// <returns></returns>
        public async Task ConnectedP2P(string forUrl, string forUserName, Guid keyChatRoom)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                        _logger.LogTrace(@"{MyName} соединение установлено с {forUrl} пользоветель {UserName} к комнате {NameRoom}", _getMyKeyConnect.UserName, forUrl, forUserName, conn?.NameRoom);
                        if (conn != null && GetStateCalling(conn) >= StateCall.CreateP2P)
                        {
                            var con = conn.Items.FirstOrDefault(x => x.UserName == forUserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                            if (con != null && con.State == StateCall.CreateP2P)
                            {
                                con.State = StateCall.Connected;
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                            }
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ConnectedP2P));
            }
        }

        /// <summary>
        /// Отменяем исходящий вызов
        /// </summary>
        /// <param name="keyChatRoom"></param>
        public async Task CancelOutCallLocal(Guid keyChatRoom)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                        _logger.LogTrace(@"{MyName} исходящий вызов отменен к комнате {NameRoom}", _getMyKeyConnect.UserName, conn?.NameRoom);
                        if (conn != null && conn.IdUiConnect == _connections.GetGuidForContextId(Context.ConnectionId) && GetStateCalling(conn) <= StateCall.Calling)
                        {
                            loadTask.Add(AddMessageForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, new(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, DispRep["CALL_CANCEL"])));
                            foreach (var connect in conn.Items.Where(x => x.UserName != _getMyKeyConnect.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)))
                            {
                                if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                {
                                    try
                                    {
                                        connect.State = StateCall.Aborted;

                                        var requestConnect = new KeyChatForUrl { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(connect.AuthorityUrl, connect.UserName) };

                                        //Отправляем отмену исходящего вызова
                                        loadTask.Add(SendLocalOrRemotaRequest(CancelOutCallRemote, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.WriteLogError(ex, $"{nameof(CancelOutCallLocal)} for {connect.AuthorityUrl}");
                                    }
                                }
                            }
                            conn.IdUiConnect = null;
                            loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CancelOutCallLocal));
            }
        }

        /// <summary>
        /// Удаленный вызов. Приходит отмена входящего вызова
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task CancelOutCallRemote(KeyChatForUrl model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                        _logger.LogTrace(@"{MyName} входящий вызов отменен пользователем {UserName} к комнате {NameRoom}", model.ForUrl.UserName, model.RemoteUrl.UserName, conn?.NameRoom);
                        if (conn != null)
                        {
                            if (GetStateCalling(conn) == StateCall.Calling || GetStateCalling(conn) <= StateCall.Disconnect)
                            {
                                loadTask.Add(AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.KeyChatRoom, new(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, DispRep["ABON_CANCEL_CALL"])));
                                foreach (var forUrl in conn.Items)
                                {
                                    forUrl.State = StateCall.Aborted;
                                    loadTask.Add(CloseP2P(model.KeyChatRoom, model.ForUrl.UserName, forUrl.AuthorityUrl, forUrl.UserName));
                                }
                            }
                            else
                            {
                                var con = conn.Items.FirstOrDefault(x => model.RemoteUrl.UserName == x.UserName && IpAddressUtilities.CompareForAuthority(model.RemoteUrl.AuthorityUrl, x.AuthorityUrl));
                                if (con != null)
                                {
                                    loadTask.Add(AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.KeyChatRoom, new(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, DispRep["ABON_END_CALL"])));
                                    con.State = StateCall.Aborted;
                                    loadTask.Add(CloseP2P(model.KeyChatRoom, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName));
                                }
                            }
                            conn.IdUiConnect = null;
                            loadTask.Add(SetInCallingConnect(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, null));
                            loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CancelOutCallRemote));
            }
        }

        /// <summary>
        /// Локальный вызов. Отменяем входящий вызов, StateCall.Disconnect
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        public async Task CancelCallLocal(Guid keyChatRoom)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                        _logger.LogTrace(@"{MyName} отмена входящего вызов к комнате {NameRoom}", _getMyKeyConnect.UserName, conn?.NameRoom);
                        if (conn != null && conn.IdUiConnect == null)
                        {
                            foreach (var connect in conn.Items.Where(x => _getMyKeyConnect.UserName != x.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)))
                            {
                                if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                {
                                    try
                                    {
                                        var requestConnect = new KeyChatForUrl { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(connect.AuthorityUrl, connect.UserName) };

                                        loadTask.Add(SendLocalOrRemotaRequest(CancelCallRemote, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.WriteLogError(ex, $"{nameof(CancelCallLocal)} for {connect.AuthorityUrl}");
                                    }
                                }
                            }
                            loadTask.Add(AddMessageForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, new(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, DispRep["IN_CALL_CANCEL"])));
                            loadTask.Add(SetInCallingConnect(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, null));
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CancelCallLocal));
            }
        }

        /// <summary>
        /// Удаленный вызов. Абонент отказался от вызова
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        public async Task CancelCallRemote(KeyChatForUrl model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                        _logger.LogTrace(@"{MyName} абонент {UserName} с IP {forUrl} отказался от вызова к комнате {NameRoom}", model.ForUrl.UserName, model.RemoteUrl.UserName, model.RemoteUrl.AuthorityUrl, conn?.NameRoom);
                        if (conn != null)
                        {
                            var con = conn.Items.FirstOrDefault(x => model.RemoteUrl.UserName == x.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));

                            if (con != null)
                            {
                                if (con.State >= StateCall.CreateP2P)
                                {
                                    loadTask.Add(CloseP2P(model.KeyChatRoom, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName));
                                }
                                con.State = StateCall.Aborted;
                            }
                            loadTask.Add(AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.KeyChatRoom, new(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, DispRep["REFUSED_CALL"])));
                            loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CancelCallRemote));
            }
        }

        /// <summary>
        /// Локальный вызов. Завершаем вызов к комнате для текущего подключения
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        public async Task CloseCallAction(Guid keyChatRoom)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                        _logger.LogTrace(@"{MyName} Завершаем вызов к комнате {NameRoom}", _getMyKeyConnect.UserName, conn?.NameRoom);
                        if (conn != null && conn.IdUiConnect == _connections.GetGuidForContextId(Context.ConnectionId))
                        {
                            if (GetStateCalling(conn) > StateCall.Disconnect)
                            {
                                conn.Items.ForEach(x =>
                                {
                                    x.State = StateCall.Disconnect;
                                });

                                foreach (var connect in conn.Items.Where(x => _getMyKeyConnect.UserName != x.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect?.AuthorityUrl, x.AuthorityUrl)))
                                {
                                    if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                                    {
                                        try
                                        {
                                            var requestConnect = new KeyChatForUrl { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(connect.AuthorityUrl, connect.UserName) };

                                            loadTask.Add(SendLocalOrRemotaRequest(CloseCall, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl));
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex.Message);
                                        }
                                    }
                                }
                                loadTask.Add(AddMessageForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, new(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, DispRep["CALL_ENDED"])));
                                loadTask.Add(SendUpdateChatRoom(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, conn));
                                conn.IdUiConnect = null;
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CloseCallAction));
            }
        }

        /// <summary>
        /// Удаленный вызов. Абонент завершил вызов
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        public async Task CloseCall(KeyChatForUrl model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.RemoteUrl?.UserName))
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        var conn = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                        _logger.LogTrace(@"{MyName} абонент {UserName} с IP {forUrl} завершил вызов к комнате {NameRoom}", model.ForUrl.UserName, model.RemoteUrl.UserName, model.RemoteUrl.AuthorityUrl, conn?.NameRoom);
                        if (conn != null && conn.IdUiConnect != null && _connections.AnyGuid(conn.IdUiConnect) && GetStateCalling(conn) >= StateCall.Calling)
                        {
                            loadTask.Add(AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.KeyChatRoom, new(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, DispRep["ABON_END_CALL"])));

                            var con = conn.Items.FirstOrDefault(x => model.RemoteUrl.UserName == x.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.RemoteUrl.AuthorityUrl));

                            if (con != null && con.State >= StateCall.Calling)
                            {
                                if (con.State >= StateCall.CreateP2P)
                                {
                                    loadTask.Add(CloseP2P(model.KeyChatRoom, model.ForUrl.UserName, model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName));
                                }
                                con.State = StateCall.Disconnect;
                            }

                            if (!conn.Items.Any(x => x.State >= StateCall.Calling))
                            {
                                conn.IdUiConnect = null;
                            }
                            loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, conn));
                        }
                    }

                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CloseCall));
            }
        }

        /// <summary>
        /// Закрываем Р2Р подключение
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="myUser"></param>
        /// <param name="forUrl"></param>
        /// <param name="forUser"></param>
        /// <returns></returns>
        async Task CloseP2P(Guid keyChatRoom, string? myUser, string? forUrl, string? forUser)
        {
            try
            {
                if (!string.IsNullOrEmpty(forUser) && !string.IsNullOrEmpty(myUser))
                {
                    var conn = FindChatForKey(myUser, keyChatRoom);
                    _logger.LogTrace(@"{MyName} закрываем P2P к комнате {NameRoom} для {forUrl}", myUser, conn?.NameRoom, forUrl);
                    if (!string.IsNullOrEmpty(forUrl) && conn != null && conn.IdUiConnect != null && _connections.AnyGuid(conn.IdUiConnect))
                    {
                        await Clients.Clients(_connections.GetContextIdForGuid(conn.IdUiConnect)).CloseP2P(forUrl, forUser, keyChatRoom);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CloseP2P));
            }
        }

        /// <summary>
        /// Удаленный вызов. Добавляем комнату
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task CreateChatRoom(JoinModel model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.ForUrl?.UserName) && !string.IsNullOrEmpty(model.ForUrl.AuthorityUrl))
                {
                    if (!AllChats.Any(x => x.UserName == model.ForUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.ForUrl.AuthorityUrl)))
                    {
                        AllChats.Add(new(model.ForUrl.AuthorityUrl, model.ForUrl.UserName));
                    }
                    List<Task> loadTask = new();

                    lock (AllChats)
                    {
                        var chats = AllChats.First(x => x.UserName == model.ForUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.ForUrl.AuthorityUrl)).Chats;

                        if (!chats.Any(x => x.Key == model.Connect.Key))
                        {
                            //удаляем дубликат
                            if ((model.Connect.IsDefault == true && model.Connect.Items.Count == 2 && FindChatListForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName).Any(x => x.Items.Select(i => $"{i.AuthorityUrl}&{i.UserName}").OrderBy(o => o).SequenceEqual(model.Connect.Items.Select(i => $"{i.AuthorityUrl}&{i.UserName}").OrderBy(o => o)))))
                            {
                                var firtsElem = FindChatListForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName).First(x => x.Items.Select(i => $"{i.AuthorityUrl}&{i.UserName}").OrderBy(o => o).SequenceEqual(model.Connect.Items.Select(i => $"{i.AuthorityUrl}&{i.UserName}").OrderBy(o => o)));

                                AllChats.First(x => x.UserName == model.ForUrl.UserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, model.ForUrl.AuthorityUrl)).Chats.RemoveAll(x => x.Key == firtsElem.Key);
                                loadTask.Add(DeleteChatForKey(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, firtsElem.Key));
                            }
                            var request = new ChatAndUserKey()
                            {
                                UserKey = new()
                                {
                                    AuthorityUrl = model.ForUrl.AuthorityUrl,
                                    UserName = model.ForUrl.UserName,
                                },
                                Chat = ToProtoModel(model.Connect)
                            };
                            var b = _pods.AddChat(request);
                            if (b?.Value == true)
                            {
                                chats.Add(new ChatInfo(model.Connect));
                                var con = chats.First(x => x.Key == model.Connect.Key);
                                if (con.IsDefault == true && !string.IsNullOrEmpty(model.RemoteUrl?.AuthorityUrl))
                                {
                                    con.NameRoom = $"{IpAddressUtilities.GetHost(model.RemoteUrl.AuthorityUrl)} - {model.RemoteUrl.UserName}";
                                }
                                loadTask.Add(SendUpdateChatRoom(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, con));
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CreateChatRoom));
            }
        }

        /// <summary>
        /// Ждем изменения статуса для пользователя, иначе ставим статус Error
        /// </summary>
        /// <param name="keyChatRoom">ключ комнаты</param>
        /// <param name="myUrl">ip адрес инициатора</param>
        /// <param name="myUser">имя инициатора</param>
        /// <param name="forUrl">для кого ожидание (ip адрес)</param>
        /// <param name="forUser">для кого ожидание (имя)</param>
        /// <param name="loadState">Статус который должен изменится</param>
        /// <param name="loadSecond">Время ожидания в секундах (default 30s)</param>
        /// <param name="nextAction">Действие в случаи невыполнения (keyChatRoom, urlLoad)</param>
        /// <returns></returns>
        async Task LoadAnswerForUrl(Guid keyChatRoom, string? myUrl, string? myUser, string? forUrl, string? forUser, StateCall loadState, int? loadSecond = 30, Action<Guid, string, string, string>? nextAction = null)
        {
            try
            {
                var conn = FindChatForKey(myUser, myUrl, keyChatRoom);
                var con = conn?.Items.FirstOrDefault(i => IpAddressUtilities.CompareForAuthority(i.AuthorityUrl, forUrl) && i.UserName == forUser);
                _logger.LogTrace(@"{MyName} ожидание изменение состояния {State} для {forUrl} пользователь {UserName} к комнате {NameRoom}", myUser, loadState, forUrl, forUser, conn?.NameRoom);
                if (conn != null && con != null)
                {
                    int countLoad = (loadSecond ?? 30) * 100;
                    while (countLoad > 0 && con.State == loadState)
                    {
                        await Task.Delay(10);
                        countLoad--;
                    }

                    _logger.LogTrace(@"{MyName} завершение ожидания изменение состояния ({OldState} - {NewState}) для {forUrl}, пользователь {userName}, к комнате {NameRoom}", myUser, loadState, con.State, forUrl, forUser, conn.NameRoom);

                    if (con.State == loadState)
                    {
                        con.State = StateCall.Error;
                        nextAction?.Invoke(keyChatRoom, myUser ?? string.Empty, forUrl ?? string.Empty, forUser ?? string.Empty);
                    }
                    await SendUpdateChatRoom(myUrl, myUser, conn);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(LoadAnswerForUrl));
            }
        }
        /// <summary>
        /// Запускаем ожидание действия на входящий вызов
        /// </summary>
        /// <param name="forUrl">кому был отправлен запрос</param>
        /// <param name="userName"></param>
        /// <param name="keyChatRoom"></param>
        /// <returns></returns>
        async Task LoadAnswerCall(string? forUrl, string? userName, Guid keyChatRoom)
        {
            var conn = FindChatForKey(userName, forUrl, keyChatRoom);
            if (conn != null)
            {
                int countLoad = 3000;
                while (countLoad > 0 && GetStateCalling(conn) == StateCall.Disconnect)
                {
                    await Task.Delay(10);
                    countLoad--;
                }

                await SetInCallingConnect(forUrl, userName, null);
                if (!string.IsNullOrEmpty(userName) && MissedCall != null && MissedCall.ContainsKey(userName) && MissedCall[userName].Key == keyChatRoom)
                {
                    MissedCall.Remove(userName);
                }
            }
        }
        /// <summary>
        /// Запускаем ожидание изменения состояния подключения для чата
        /// </summary>
        /// <param name="myUrl">кто запустил</param>
        /// <param name="myUser"></param>
        /// <param name="keyChatRoom"></param>
        /// <param name="loadSecond">время ожидания (default 30s)</param>
        /// <param name="nextAction">действие в случаи неудачи</param>
        /// <returns></returns>
        async Task LoadChangeStateChat(string? myUrl, string? myUser, Guid keyChatRoom, int? loadSecond = 30, Action<string, string, Guid>? nextAction = null)
        {
            try
            {
                var conn = FindChatForKey(myUser, myUrl, keyChatRoom);
                _logger.LogTrace(@"{MyName} ожидание изменение состояния для {NameRoom}", myUser, conn?.NameRoom);
                if (conn != null)
                {
                    int countLoad = (loadSecond ?? 30) * 100;
                    while (countLoad > 0 && GetStateCalling(conn) >= StateCall.Calling && GetStateCalling(conn) < StateCall.Connected)
                    {
                        await Task.Delay(10);
                        countLoad--;
                    }

                    _logger.LogTrace(@"{MyName} завершение ожидания изменение состояния ({NewState}) для {NameRoom}", myUser, GetStateCalling(conn), conn.NameRoom);

                    if (GetStateCalling(conn) != StateCall.Connected)
                    {
                        if (GetStateCalling(conn) >= StateCall.CreateP2P)
                        {
                            foreach (var item in conn.Items.Where(x => x.State >= StateCall.CreateP2P))
                            {
                                await CloseP2P(keyChatRoom, myUser, item.AuthorityUrl, item.UserName);
                            }
                        }
                        SetAllConnItemsState(conn, StateCall.Error);
                        if (nextAction != null && !string.IsNullOrEmpty(myUrl) && !string.IsNullOrEmpty(myUser))
                        {
                            nextAction.Invoke(myUrl, myUser, keyChatRoom);
                        }
                    }
                    await SendUpdateChatRoom(myUrl, myUser, conn);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(LoadChangeStateChat));
            }
        }

        /// <summary>
        /// Отправляем сообщение об ошибке установки Р2Р, закрываем подключение
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="myUser"></param>
        /// <param name="forUrl"></param>
        /// <param name="forUser"></param>
        /// <returns></returns>
        async Task ErrorConnectP2P(Guid keyChatRoom, string myUser, string forUrl, string forUser)
        {
            await AddMessageForUser(forUrl, forUser, keyChatRoom, new(forUrl, forUser, DispRep["COMMUNICATION_ERROR"]));
            await CloseP2P(keyChatRoom, myUser, forUrl, forUser);
        }

        /// <summary>
        /// Обнавляем статус для подключений в чате
        /// </summary>
        /// <param name="item"></param>
        /// <param name="state"></param>
        void SetAllConnItemsState(ChatInfo? item, StateCall state)
        {
            if (item != null)
            {
                item.Items.ForEach(x =>
                {
                    x.State = state;
                });
            }
        }
        /// <summary>
        /// Получаем общее состояние для чата
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public StateCall GetStateCalling(ChatInfo? item)
        {
            if (item != null)
            {
                if (item.Items.Any(x => x.State == StateCall.Connected || x.State == StateCall.ChangeStream))
                    return StateCall.Connected;
                else if (item.Items.Any(x => x.State == StateCall.CreateP2P))
                    return StateCall.CreateP2P;
                else if (item.Items.Any(x => x.State == StateCall.CreateAnswer))
                    return StateCall.CreateAnswer;
                else if (item.Items.Any(x => x.State == StateCall.Calling))
                    return StateCall.Calling;
                else if (item.Items.Any(x => x.State == StateCall.Error))
                    return StateCall.Error;
                else if (item.Items.Any(x => x.State == StateCall.Aborted))
                    return StateCall.Aborted;
                else return StateCall.Disconnect;
            }
            return StateCall.Disconnect;
        }

        /// <summary>
        /// Локальная рассылка сообщений
        /// </summary>
        /// <param name="authorityUrl">ip адрес пользователя</param>
        /// <param name="userName">имя пользователя</param>
        /// <param name="keyChatRoom">ключ чата</param>
        /// <param name="message">сообщение</param>
        /// <returns></returns>
        public async Task AddMessageForUser(string? authorityUrl, string? userName, Guid keyChatRoom, ChatMessage message)
        {
            try
            {
                if (!string.IsNullOrEmpty(authorityUrl) && !string.IsNullOrEmpty(userName))
                {
                    PodsProto.V1.ChatMessage newMessage = new()
                    {
                        AuthorityUrl = IpAddressUtilities.GetAuthority(message.AuthorityUrl),
                        UserName = message.UserName,
                        Date = message.Date.ToTimestamp(),
                        Message = message.Message,
                        Url = message.Url
                    };

                    var result = await _pods.AddMessageForChatAsync(new NewMessage()
                    {
                        Message = newMessage,
                        Key = new()
                        {
                            UserKey = new()
                            {
                                AuthorityUrl = IpAddressUtilities.GetAuthority(authorityUrl),
                                UserName = userName
                            },
                            ChatKey = keyChatRoom.ToString()
                        }
                    });

                    if (result?.Value == true)
                    {
                        var r = _connections.GetContextIdForLogin(IpAddressUtilities.GetAuthority(authorityUrl), userName);
                        if (r?.Count() > 0)
                        {
                            await Clients.Clients(r).AddMessageForChat(keyChatRoom, message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(AddMessageForUser));
            }
        }

        public async Task<string> StartRecord(Guid keyChatRoom)
        {
            try
            {
                if (!string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect.UserName))
                {
                    string fileName = Path.Combine(CreatePathForChat(keyChatRoom), Path.ChangeExtension(Path.GetRandomFileName(), ".video"));

                    await AddMessageForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, new()
                    {
                        AuthorityUrl = _getMyKeyConnect.AuthorityUrl,
                        UserName = _getMyKeyConnect.UserName,
                        Date = DateTime.UtcNow,
                        Message = string.Empty,
                        Url = fileName
                    });
                    return fileName;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(StartRecord));
            }
            return string.Empty;
        }

        public string CreatePathForChat(Guid keyChatRoom)
        {
            try
            {
                return CreatePathForChat(keyChatRoom, _getMyKeyConnect?.UserName);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CreatePathForChat));
            }
            return string.Empty;
        }

        string CreatePathForChat(Guid keyChatRoom, string? userName)
        {
            try
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    Regex regexUserName = new(@"[^\w]");
                    return Path.Combine(regexUserName.Replace(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)), ""), keyChatRoom.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CreatePathForChat));
            }
            return string.Empty;
        }

        /// <summary>
        /// Отправка push уведомлений
        /// </summary>
        /// <param name="pushMessage"></param>
        /// <returns></returns>
        private async Task SendPushAsync(PushMessageChat pushMessage)
        {
            try
            {
                var r = _connections.GetContextIdForLogin(pushMessage.ForUrl, pushMessage.ForUser);
                if (r?.Any() ?? false)
                {
                    _logger.LogTrace("Отправка уведомления для {name}, сообщение {message}", pushMessage.ForUser, pushMessage.Message);
                    var payload = JsonSerializer.Serialize(pushMessage);
                    await Clients.Clients(r).Fire_ShowPushNotify(payload);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendPushAsync));
            }
        }

        /// <summary>
        /// Локальный вызов. Отправляем сообщение всем пользователям в чате
        /// </summary>
        /// <param name="keyChatRoom"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(Guid keyChatRoom, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl) || string.IsNullOrEmpty(_getMyKeyConnect.UserName)) return;

                var conn = GetChatListForUser.FirstOrDefault(x => x.Key == keyChatRoom);
                if (conn != null)
                {
                    await AddMessageForUser(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, keyChatRoom, new(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName, message));
                    foreach (var connect in conn.Items.Where(x => _getMyKeyConnect.UserName != x.UserName | !IsLocalAuthorityUrl(_getMyKeyConnect.AuthorityUrl, x.AuthorityUrl)))
                    {
                        if (connect != null && !string.IsNullOrEmpty(connect.AuthorityUrl))
                        {
                            try
                            {
                                var requestCreateChat = new JoinModel { Connect = new(conn), RemoteUrl = _getMyKeyConnect, ForUrl = new(connect.AuthorityUrl, connect.UserName) };
                                var requestConnect = new KeyChatForUrlAndValue { KeyChatRoom = keyChatRoom, RemoteUrl = _getMyKeyConnect, ForUrl = new(connect.AuthorityUrl, connect.UserName), Value = message };

                                await SendLocalOrRemotaRequest(CreateChatRoom, requestCreateChat, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl);
                                await SendLocalOrRemotaRequest(SetMessage, requestConnect, _getMyKeyConnect.AuthorityUrl, connect.AuthorityUrl);
                            }
                            catch (Exception ex)
                            {
                                _logger.WriteLogError(ex, $"{nameof(SendMessage)} for {connect.AuthorityUrl}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SendMessage));
            }
        }

        /// <summary>
        /// Удаленный вызов. Получено сообщение
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task SetMessage(KeyChatForUrlAndValue model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.ForUrl?.UserName) || string.IsNullOrEmpty(model.RemoteUrl?.UserName)) return;

                await AddMessageForUser(model.ForUrl.AuthorityUrl, model.ForUrl.UserName, model.KeyChatRoom, new(model.RemoteUrl.AuthorityUrl, model.RemoteUrl.UserName, model.Value));
                var con = FindChatForKey(model.ForUrl.UserName, model.ForUrl.AuthorityUrl, model.KeyChatRoom);
                if (con != null)
                {
                    PushMessageChat pushMessage = new();
                    pushMessage.Url = $"/";
                    pushMessage.Title = con.NameRoom;
                    pushMessage.KeyChatRoom = model.KeyChatRoom.ToString();
                    pushMessage.ForUser = model.ForUrl?.UserName;
                    pushMessage.ForUrl = model.ForUrl?.AuthorityUrl;
                    pushMessage.Message = $"{GetCuName(model.RemoteUrl.AuthorityUrl)}-{model.RemoteUrl.UserName}: {string.Join("", model.Value.Take(100))}";
                    await SendPushAsync(pushMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(SetMessage));
            }
        }

        /// <summary>
        /// Получаем имя пункта управления
        /// </summary>
        /// <param name="authorityUrl"></param>
        /// <returns></returns>
        string GetCuName(string? authorityUrl)
        {
            if (!string.IsNullOrEmpty(authorityUrl))
            {
                return _pods.FindCuName(new StringValue() { Value = authorityUrl })?.Value ?? authorityUrl ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Получаем список подключенных пользователей
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string>? GetConnectUsers()
        {
            return _connections.GetAllConnectedUser();
        }

        public async Task WriteLocalContact()
        {
            try
            {
                ContactInfoList request = new();
                request.List.AddRange(await GetLocalCuInfo());
                if (request.List?.Count > 0)
                {
                    var currentList = await _pods.GetLocalContactAsync(new Empty());

                    var b = await _pods.AddLocalContactAsync(request);
                    if (b?.Value == true)
                    {
                        var addData = request.List.Select(item => new SharedLibrary.Models.ContactInfo(item.NameCu, item.AuthorityUrl, item.UserName, item.StaffId, item.LastActive?.ToDateTime()) { Type = (SharedLibrary.Models.TypeContact)item.Type });

                        var deletedItems = currentList?.List.ExceptBy(request.List.Select(x => x.UserName), x => x.UserName);

                        if (deletedItems?.Any() ?? false)
                        {
                            await RemoveConnectInfoForAllChat(deletedItems);
                            currentList?.List.RemoveAll(x => deletedItems.Contains(x));
                        }

                        var updateData = currentList?.List.ExceptBy(request.List.Select(x => $"{x.NameCu}{x.AuthorityUrl}&{x.UserName}"), x => $"{x.NameCu}{x.AuthorityUrl}&{x.UserName}");

                        if (updateData?.Any() ?? false)
                        {
                            lock (AllChats)
                            {
                                foreach (var item in updateData)
                                {
                                    var f = request.List.FirstOrDefault(x => x.UserName == item.UserName);
                                    if (f != null)
                                    {
                                        var items = AllChats.SelectMany(x => x.Chats).SelectMany(x => x.Items).Where(x => x.AuthorityUrl == item.AuthorityUrl && x.UserName == item.UserName);
                                        if (items != null)
                                        {
                                            foreach (var con in items)
                                            {
                                                con.AuthorityUrl = f.AuthorityUrl;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        await Clients.All.AddOrUpdateContactList(addData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(WriteLocalContact));
            }
        }


        /// <summary>
        /// Добавляем пользователей с подчиненного ПУ
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public async Task WriteRemoteContact(IEnumerable<SharedLibrary.Models.ContactInfo>? list)
        {
            if (list != null)
            {
                try
                {
                    ContactInfoList request = new();

                    var forUrl = list.FirstOrDefault()?.AuthorityUrl;
                    if (!string.IsNullOrEmpty(forUrl))
                    {
                        var currentList = await _pods.GetRemoteContactForUrlAsync(new StringValue() { Value = forUrl });

                        request.List.AddRange(list.Select(item => new PodsProto.V1.ContactInfo()
                        {
                            NameCu = item.Name,
                            AuthorityUrl = item.AuthorityUrl,
                            UserName = item.UserName,
                            StaffId = item.StaffId,
                            LastActive = item.LastActive?.ToTimestamp(),
                            Type = PodsProto.V1.TypeContact.Remote
                        }));
                        var b = await _pods.AddRemoteContactAsync(request);

                        if (b?.Value == true)
                        {
                            var addData = request.List.Select(item => new SharedLibrary.Models.ContactInfo(item.NameCu, item.AuthorityUrl, item.UserName, item.StaffId, item.LastActive?.ToDateTime()) { Type = (SharedLibrary.Models.TypeContact)item.Type });

                            var deletedItems = currentList?.List.ExceptBy(request.List.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}");
                            if (deletedItems?.Any() ?? false)
                            {
                                await RemoveConnectInfoForAllChat(deletedItems);
                            }
                            await Clients.Others.AddOrUpdateContactList(addData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, nameof(WriteRemoteContact));
                }
            }
        }

        public async Task SetCurrentRemoteContactForIp(IEnumerable<string>? list)
        {
            if (list != null)
            {
                try
                {
                    CurrentRemoteCuArray request = new();
                    request.List.AddRange(list);
                    await _pods.SetCurrentRemoteContactForIpAsync(request);
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, nameof(SetCurrentRemoteContactForIp));
                }
            }
        }

        async Task RemoveConnectInfoForAllChat(IEnumerable<PodsProto.V1.ContactInfo>? deletedItems)
        {
            try
            {
                if (deletedItems?.Any() ?? false)
                {
                    List<Task> loadTask = new();
                    lock (AllChats)
                    {
                        foreach (var item in AllChats)
                        {
                            foreach (var chat in item.Chats)
                            {
                                var first = chat.Items.FirstOrDefault(x => deletedItems.Any(i => i.UserName == x.UserName && i.AuthorityUrl == x.AuthorityUrl));
                                if (first != null)
                                {
                                    if (first.State >= StateCall.Calling)
                                    {
                                        if (!string.IsNullOrEmpty(first.AuthorityUrl) && !string.IsNullOrEmpty(first.UserName) && chat.IdUiConnect != null && _connections.AnyGuid(chat.IdUiConnect))
                                        {
                                            loadTask.Add(Clients.Clients(_connections.GetContextIdForGuid(chat.IdUiConnect)).CloseP2P(first.AuthorityUrl, first.UserName, chat.Key));
                                        }
                                    }
                                    chat.Items.Remove(first);
                                    loadTask.Add(SendUpdateChatRoom(item.AuthorityUrl, item.UserName, chat));
                                }
                            }
                        }
                    }
                    await Task.WhenAll(loadTask);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(RemoveConnectInfoForAllChat));
            }
        }

        /// <summary>
        /// Добавляем или редактируем пользователя пу
        /// </summary>
        /// <param name="newCu"></param>
        /// <returns></returns>
        public async Task AddOrUpdateContact(SharedLibrary.Models.ContactInfo? newCu)
        {
            if (newCu != null && !string.IsNullOrEmpty(newCu.AuthorityUrl) && !string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl))
            {
                try
                {
                    newCu.Type = SharedLibrary.Models.TypeContact.Manual;
                    var b = _pods.AddContactForUser(new()
                    {
                        UserKey = new()
                        {
                            AuthorityUrl = _getMyKeyConnect.AuthorityUrl,
                            UserName = _getMyKeyConnect.UserName
                        },
                        Contact = new()
                        {
                            AuthorityUrl = newCu.AuthorityUrl,
                            UserName = newCu.UserName,
                            NameCu = newCu.Name,
                        }
                    });
                    if (b?.Value == true)
                    {
                        await Clients.Clients(_connections.GetContextIdForLogin(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName)).AddOrUpdateContact(newCu);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, nameof(AddOrUpdateContact));
                }
            }
        }

        public async Task RemoveNoReadMessage(Guid? keyChatRoom)
        {
            if (keyChatRoom != null && !string.IsNullOrEmpty(_getMyKeyConnect?.AuthorityUrl))
            {
                try
                {
                    var r = _connections.GetContextIdForLogin(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName);
                    var b = await _pods.RemoveNoReadMessagesAsync(new()
                    {
                        UserKey = new()
                        {
                            AuthorityUrl = _getMyKeyConnect.AuthorityUrl,
                            UserName = _getMyKeyConnect.UserName
                        },
                        ChatKey = keyChatRoom.ToString()
                    });
                    if (b?.Value == true)
                    {
                        if (r?.Count() > 0)
                        {
                            await Clients.Clients(r).RemoveNoReadMessage(keyChatRoom);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, nameof(RemoveNoReadMessage));
                }
            }
        }


        /// <summary>
        /// Удаляем пользователя пу
        /// </summary>
        /// <param name="authorityUrl"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<bool> DeleteContactForUser(string? authorityUrl, string? userName)
        {
            if (!string.IsNullOrEmpty(authorityUrl) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(_getMyKeyConnect?.UserName))
            {
                try
                {
                    var b = _pods.DeleteContactForUser(new DeleteContactKey()
                    {
                        ContactKey = new()
                        {
                            AuthorityUrl = authorityUrl,
                            UserName = userName
                        },
                        UserKey = new()
                        {
                            AuthorityUrl = _getMyKeyConnect.AuthorityUrl,
                            UserName = _getMyKeyConnect.UserName
                        }
                    });
                    if (b?.Value == true)
                    {
                        await Clients.Clients(_connections.GetContextIdForLogin(_getMyKeyConnect.AuthorityUrl, _getMyKeyConnect.UserName)).DeleteContactNotify(authorityUrl, userName);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLogError(ex, nameof(DeleteContactForUser));
                }
            }
            return false;
        }

        /// <summary>
        /// Входящие подключение к серверу
        /// </summary>
        /// <returns></returns>
        public override Task OnConnectedAsync()
        {
            try
            {
                StringValues result = new();
                var context = Context.GetHttpContext();

                var userName = context?.User.Identity?.Name;

                if (!string.IsNullOrEmpty(userName))
                {
                    context?.Request.Headers.TryGetValue("Origin", out result);
                    var urlConnect = IpAddressUtilities.GetAuthority(result.FirstOrDefault());

                    if (urlConnect.Contains("localhost") || urlConnect.Contains("127.0.0.1") || urlConnect.Contains("0.0.0.0"))
                    {
                        try
                        {
                            urlConnect = GetLocalAuthorityUrl().Result;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(@"Error get local ip: {message}", e.Message);
                        }
                    }

                    StringValues valuesUi = new();
                    var idUi = context?.Request.Query.TryGetValue(nameof(CookieName.AppId), out valuesUi);
                    Guid.TryParse(valuesUi.FirstOrDefault(), out var guidUi);

                    _logger.LogTrace(@"Новое подключение к хабу для {forUrl}, пользователь {userName}, guid {guid}, назначенный ID {Id}, локальный хост {Host}", urlConnect, userName, guidUi, Context.ConnectionId, context?.Request.Host);

                    if (!string.IsNullOrEmpty(urlConnect))
                        _connections.Add(urlConnect, Context.ConnectionId, guidUi, userName);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(OnConnectedAsync));
            }
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// Потеря подключения к серверу
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                StringValues result = new();
                var context = Context.GetHttpContext();
                context?.Request.Headers.TryGetValue("Origin", out result);
                var urlConnect = IpAddressUtilities.GetAuthority(result.FirstOrDefault());

                StringValues valuesUi = new();
                var idUi = context?.Request.Query.TryGetValue(nameof(CookieName.AppId), out valuesUi);
                Guid.TryParse(valuesUi.FirstOrDefault(), out var guidUi);

                _logger.LogTrace(@"Закрыто подключение к хабу для {forUrl}, guid {guid}, назначенный ID {Id}, локальный хост {Host}", urlConnect, guidUi, Context.ConnectionId, context?.Request.Host);

                _connections.RemoveForConnectId(Context.ConnectionId);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(OnDisconnectedAsync));
            }
            return base.OnConnectedAsync();
        }
    }
}

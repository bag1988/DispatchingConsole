﻿using System.ComponentModel;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;
using SensorM.GsoCore.SharedLibrary.Interfaces;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;

namespace DispatchingConsole.Client.WebRTC
{
    public class CommunicationService : IChatHub, IAsyncDisposable
    {
        private IJSObjectReference? _jsModuleRtc;

        public ChatInfo? SelectConnect
        {
            get
            {
                return _SelectConnect;
            }
        }

        int SkipData = 0;
        int TakeData = 0;
        int AllCount = 0;
        ChatInfo? _SelectConnect { get; set; }

        public bool LoadChangeSelectValue = true;

        ViewPodsMessageFiltr FiltrModel = new();

        public ChatInfo? InCallingConnect { get; set; }

        public string? UserName { get; set; }

        public string[] _audioExt = [".audio", ".mp3", ".wav", ".mpeg"];
        public string[] _videoExt = [".webm", ".video", ".mp4"];
        public string[] _imgExt = [".png", ".gif", ".jpeg", ".svg"];
        public List<ChatInfo> ConnectList { get; set; } = new();

        public bool LoadConnectList = true;
        public bool IsEditItemsConnect { get; set; } = false;
        public bool isDeleteChatRoom = false;
        private DotNetObjectReference<CommunicationService>? _jsThis { get; set; }

        public List<ChatMessage>? CurrentChatMessages { get; set; } = null;

        public string TempMessage = string.Empty;

        IJSObjectReference? playerCaller = null;

        IJSObjectReference? _observer = null;

        IJSObjectReference? _push = null;

        readonly NavigationManager MyNavigationManager;

        readonly IJSRuntime JSRun;

        public Action? CallBackUpdateView { get; set; }

        HubConnection _myHub = default!;

        public List<ContactInfo> ContactList { get; set; } = new();

        string? _appId = string.Empty;

        public Dictionary<Guid, int> NoReadMessages = new();

        readonly ILogger<CommunicationService> _logger;

        readonly int BufferSizeSignal = 24000;
        private System.Threading.Channels.Channel<byte[]>? channelForRecord;
        private System.Threading.Channels.Channel<byte[]>? channelForUpload;

        public string MyAuthorityUrl = string.Empty;

        public CommunicationService(NavigationManager myNavigationManager, IJSRuntime jSRuntime, ILogger<CommunicationService> logger)
        {
            MyNavigationManager = myNavigationManager;
            JSRun = jSRuntime;
            _logger = logger;
        }

        public async Task OnInitializedCommunicationAsync(string localIdPlayer, string remoteIdPlayerArray)
        {
            try
            {
                LoadConnectList = true;
                LoadChangeSelectValue = true;
                CallBackUpdateView?.Invoke();
                _jsModuleRtc = await JSRun.InvokeAsync<IJSObjectReference>("import", $"./js/CommunicationService.js?v={AssemblyNames.GetVersionPKO}");
                _observer = await JSRun.InvokeAsync<IJSObjectReference>("import", $"./js/CreateObserver.js?v={AssemblyNames.GetVersionPKO}");
                _push = await JSRun.InvokeAsync<IJSObjectReference>("import", $"./js/push.js?v={AssemblyNames.GetVersionPKO}");
                _jsThis = DotNetObjectReference.Create(this);

                _ = _jsModuleRtc.InvokeVoidAsync("initialize", _jsThis, localIdPlayer, remoteIdPlayerArray);
                await _observer.InvokeVoidAsync("CreateObserver.init", _jsThis, nameof(CallBackObserver));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task SetSelectValue(ChatInfo? value)
        {
            LoadChangeSelectValue = true;
            try
            {
                if (!value?.Equals(_SelectConnect) ?? true)
                {
                    _SelectConnect = value;
                    await ResetChatMessage();
                    CallBackUpdateView?.Invoke();
                    if (value != null)
                    {
                        await InitMessageForChat(value.Key);
                        await SendCoreHubAsync("RemoveNoReadMessage", [value.Key]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка установки активного чата: {message}", ex.Message);
            }
            LoadChangeSelectValue = false;
        }


        async Task ResetChatMessage()
        {
            CurrentChatMessages = null;
            SkipData = 0;
            TakeData = 100;
            AllCount = 0;
            if (_observer != null)
            {
                await _observer.InvokeVoidAsync("CreateObserver.stopObserver");
            }

        }

        /// <summary>
        /// Запуск прослушивания локального сервера
        /// </summary>
        /// <returns></returns>
        public async Task OnStartListenLocalHub(string? appId)
        {
            try
            {
                LoadConnectList = true;
                ConnectList = new();

                _appId = appId;
                if (_myHub != null)
                {
                    await _myHub.StopAsync();
                    await _myHub.DisposeAsync();
                }
                var absoluteUri = MyNavigationManager.ToAbsoluteUri($"/CommunicationChatHub?{nameof(CookieName.AppId)}={appId}");
                _myHub = new HubConnectionBuilder().WithUrl(absoluteUri).WithAutomaticReconnect().Build();
                _myHub.SubscribeViaInterface(this, typeof(IChatHub));

                if (_myHub.State != HubConnectionState.Connected)
                {
                    await _myHub.StartAsync();
                }
                var newData = await InvokeCoreHubAsync<List<ChatInfo>?>("GetConnectionList", Array.Empty<object>());
                if (newData?.Count > 0)
                {
                    ConnectList.AddRange(newData.ExceptBy(ConnectList.Select(x => x.Key), (x) => x.Key));
                }

                var allContactList = await InvokeCoreHubAsync<List<ContactInfo>?>("GetAllContactForUser", Array.Empty<object>());
                if (allContactList?.Count > 0)
                {
                    ContactList = allContactList;
                }

                NoReadMessages = await InvokeCoreHubAsync<Dictionary<Guid, int>?>("GetCountNoReadMessages", Array.Empty<object>()) ?? new();

                MyAuthorityUrl = await InvokeCoreHubAsync<string?>("GetLocalAuthorityUrl", Array.Empty<object>()) ?? MyNavigationManager.BaseUri;

                LoadConnectList = false;
                LoadChangeSelectValue = false;
                if (_SelectConnect == null && ConnectList.Count > 0)
                {
                    await SetSelectValue(ConnectList.FirstOrDefault());
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(@"Ошибка запуска прослушивания локального сервера, {message}", ex.Message);
            }
        }

        public async Task OnLoadMissedCall()
        {
            try
            {
                var newData = await _myHub.InvokeAsync<ChatInfo?>("GetMissedCall");
                if (newData != null)
                {
                    InCallingConnect = newData;
                }
                CallBackUpdateView?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        [Description(DaprMessage.PubSubName)]
        public Task DeleteContactNotify(string? authorityUrl, string? userName)
        {
            if (!string.IsNullOrEmpty(authorityUrl) && !string.IsNullOrEmpty(userName))
            {
                lock (ContactList)
                {
                    if (ContactList.Any(x => IpAddressUtilities.CompareForAuthority(authorityUrl, x.AuthorityUrl) && x.UserName == userName))
                    {
                        ContactList.RemoveAll(x => IpAddressUtilities.CompareForAuthority(authorityUrl, x.AuthorityUrl) && x.UserName == userName);
                    }
                }
            }
            CallBackUpdateView?.Invoke();
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_ShowPushNotify(string? json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json) && _push != null)
                {
                    await _push.InvokeVoidAsync("SendPush", json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public Task UpdateListActiveContact(string authorityUrl, string userName, DateTime lastActive)
        {
            try
            {
                lock (ContactList)
                {
                    var first = ContactList.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl) && x.UserName == userName);
                    if (first != null)
                    {
                        first.LastActive = lastActive;
                        CallBackUpdateView?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task AddOrUpdateContactList(IEnumerable<ContactInfo>? list)
        {
            try
            {
                if (list != null)
                {
                    bool isUpdateChatRoom = false;
                    lock (ContactList)
                    {
                        List<ContactInfo>? newData = null;
                        List<ContactInfo>? updateData = null;
                        List<ContactInfo>? deleteData = null;
                        if (list.First().Type == TypeContact.Local)
                        {
                            deleteData = ContactList.Where(x => x.Type == TypeContact.Local).ToList().ExceptBy(list.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}").ToList();

                            newData = list.ExceptBy(ContactList.Where(x => x.Type == TypeContact.Local).Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}").ToList();

                            updateData = ContactList.Where(x => x.Type == TypeContact.Local).IntersectBy(list.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}").ToList();
                        }
                        else
                        {
                            deleteData = ContactList.Where(x => x.Type != TypeContact.Local && list.Any(l => l.AuthorityUrl == x.AuthorityUrl)).ToList().ExceptBy(list.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}").ToList();

                            newData = list.ExceptBy(ContactList.Where(x => x.Type != TypeContact.Local).Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}").ToList();

                            updateData = ContactList.Where(x => x.Type != TypeContact.Local).IntersectBy(list.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}").ToList();
                        }


                        if (deleteData.Any())
                        {
                            _logger.LogTrace("Удаление {count} контактов ({type})", deleteData.Count, list.First().Type);
                            ContactList.RemoveAll(x => deleteData.Contains(x));

                            if (deleteData.Any(x => x.UserName == UserName && IsMyAuthorityUrl(x.AuthorityUrl)))
                            {
                                JSRun.InvokeVoidAsync("localStorage.removeItem", CookieName.Token);
                                MyNavigationManager.NavigateTo("/", true, true);
                            }
                        }

                        if (newData?.Count > 0)
                        {
                            _logger.LogTrace("Добавление {count} контактов ({type})", newData.Count, list.First().Type);
                            ContactList.AddRange(newData);
                            if (newData.First().Type == TypeContact.Local)
                            {
                                isUpdateChatRoom = true;
                            }
                        }
                        if (updateData?.Any() ?? false)
                        {
                            _logger.LogTrace("Обновление {count} контактов ({type})", updateData.Count, list.First().Type);
                            foreach (var item in updateData)
                            {
                                var first = list.FirstOrDefault(x => x.AuthorityUrl == item.AuthorityUrl && x.UserName == item.UserName);
                                if (first != null)
                                {
                                    var indexElem = ContactList.IndexOf(item);
                                    ContactList.Remove(item);
                                    ContactList.Insert(indexElem, first);
                                }
                            }
                        }
                    }

                    if (isUpdateChatRoom)
                    {
                        var newData = await InvokeCoreHubAsync<List<ChatInfo>?>("GetConnectionList", Array.Empty<object>());
                        _logger.LogTrace("Получили список комнат ({count}) для новых контактов", newData?.Count);
                        if (newData?.Count > 0)
                        {
                            ConnectList.AddRange(newData.ExceptBy(ConnectList.Select(x => x.Key), (x) => x.Key));
                        }
                    }

                    CallBackUpdateView?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public Task AddOrUpdateContact(ContactInfo? newCu)
        {
            try
            {
                if (newCu != null && !string.IsNullOrEmpty(newCu.AuthorityUrl))
                {
                    lock (ContactList)
                    {
                        if (!ContactList.Any(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, newCu.AuthorityUrl) && x.UserName == newCu.UserName))
                        {
                            ContactList.Add(newCu);
                        }
                        else
                        {
                            var first = ContactList.First(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, newCu.AuthorityUrl) && x.UserName == newCu.UserName);

                            first.LastActive = newCu.LastActive;
                            first.Type = newCu.Type;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            CallBackUpdateView?.Invoke();
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task AddMessageForChat(Guid keyChatRoom, ChatMessage message)
        {
            try
            {
                if (SelectConnect?.Key == keyChatRoom)
                {
                    CurrentChatMessages ??= new();
                    CurrentChatMessages.Add(message);
                    AllCount++;
                    await SendCoreHubAsync("RemoveNoReadMessage", [keyChatRoom]);
                }
                else
                {
                    await AddNoReadMessage(keyChatRoom);
                }
                var f = ConnectList.FirstOrDefault(x => x.Key == keyChatRoom);
                if (f != null && ConnectList.IndexOf(f) > 0)
                {
                    ConnectList.Remove(f);
                    ConnectList.Insert(0, f);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            CallBackUpdateView?.Invoke();

            if (SelectConnect?.Key == keyChatRoom)
            {
                await ScrollToElement(true);
                if (!string.IsNullOrEmpty(message.Url) && _audioExt.Contains(Path.GetExtension(message.Url).ToLower()))
                {
                    await Task.Yield();
                    await ReloadLastPlayer(message.Url);
                }
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task SetInCallingConnect(ChatInfo? newConnect)
        {
            try
            {

                if (newConnect != null)
                {
                    InCallingConnect = newConnect;
                    try
                    {
                        if (playerCaller == null)
                        {
                            _ = JSRun.InvokeAsync<IJSObjectReference>("playSoundForUrl", "/caller.wav", true).AsTask().ContinueWith(async audioPlayer =>
                            {
                                playerCaller = await audioPlayer;
                            });
                        }
                        else
                            await playerCaller.InvokeVoidAsync("play");
                    }
                    catch
                    {
                        _logger.LogError("Ошибка воспроизведения файла '/caller.wav'");
                    }
                }
                else
                {
                    _ = playerCaller?.InvokeVoidAsync("pause");
                    InCallingConnect = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SetInCallingConnect {ex.Message}");
            }
            CallBackUpdateView?.Invoke();

        }

        [Description(DaprMessage.PubSubName)]
        public async Task DeleteChatForKey(Guid keyChatRoom)
        {
            try
            {

                if (SelectConnect?.Key == keyChatRoom)
                {
                    await SetSelectValue(null);
                    IsEditItemsConnect = false;
                    isDeleteChatRoom = false;
                }
                ConnectList.RemoveAll(x => x.Key == keyChatRoom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            CallBackUpdateView?.Invoke();

        }

        [Description(DaprMessage.PubSubName)]
        public Task UpdateChatRoom(ChatInfo newConnect)
        {
            try
            {
                var first = ConnectList.FirstOrDefault(x => x.Key == newConnect.Key);
                if (first != null)
                {
                    first.IdUiConnect = newConnect.IdUiConnect;
                    first.OutTypeConn = newConnect.OutTypeConn;
                    first.Items.Clear();
                    first.Items.AddRange(newConnect.Items);
                }
                else
                {
                    ConnectList.Add(newConnect);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            CallBackUpdateView?.Invoke();
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task GoConnectionP2P(string forUrl, string forUserName, Guid keyChatRoom)
        {
            try
            {
                var conn = ConnectList.FirstOrDefault(y => y.Key == keyChatRoom);

                if (conn != null)
                {
                    var con = conn.Items.FirstOrDefault(x => x.UserName == forUserName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                    if (_jsModuleRtc != null && con != null)
                    {
                        var offer = await _jsModuleRtc.InvokeAsync<string?>("callAction", IpAddressUtilities.GetAuthority(forUrl), forUserName, keyChatRoom, conn.OutTypeConn == TypeConnect.Video);
                        if (!string.IsNullOrEmpty(offer))
                        {
                            await SendCoreHubAsync("SendOfferForClient", [forUrl, forUserName, keyChatRoom, offer ?? string.Empty]);
                        }
                        else
                        {
                            await CloseP2P(forUrl, forUserName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task SetRemoteOfferForUrl(string forUrl, string forUser, Guid keyChatRoom, string offer)
        {
            try
            {
                var conn = ConnectList.FirstOrDefault(y => y.Key == keyChatRoom);

                if (conn != null)
                {
                    var con = conn.Items.FirstOrDefault(x => x.UserName == forUser && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                    if (_jsModuleRtc != null && con != null)
                    {
                        var answer = await _jsModuleRtc.InvokeAsync<string?>("processOffer", IpAddressUtilities.GetAuthority(forUrl), forUser, keyChatRoom, offer, conn.OutTypeConn == TypeConnect.Video);
                        if (!string.IsNullOrEmpty(answer))
                        {
                            await SendCoreHubAsync("SendAnswerForRemoteClient", [forUrl, forUser, keyChatRoom, answer ?? string.Empty]);
                        }
                        else
                        {
                            await CloseP2P(forUrl, forUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task SetChangeRemoteOfferForUrl(string forUrl, string forUser, Guid keyChatRoom, string offer)
        {
            try
            {
                var conn = ConnectList.FirstOrDefault(y => y.Key == keyChatRoom);

                if (conn != null)
                {
                    var con = conn.Items.FirstOrDefault(x => x.UserName == forUser && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                    if (_jsModuleRtc != null && con != null)
                    {
                        var answer = await _jsModuleRtc.InvokeAsync<string?>("StartAnswerForChangeStream", IpAddressUtilities.GetAuthority(forUrl), forUser, keyChatRoom, offer);
                        if (!string.IsNullOrEmpty(answer))
                        {
                            await SendCoreHubAsync("SendAnswerForRemoteClient", [forUrl, forUser, keyChatRoom, answer ?? string.Empty]);
                        }
                        else
                        {
                            await CloseP2P(forUrl, forUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task SetRemoteAnswerForUrl(string forUrl, string forUser, Guid keyChatRoom, string answer)
        {
            try
            {
                var conn = ConnectList.FirstOrDefault(y => y.Key == keyChatRoom);
                if (conn != null)
                {
                    var con = conn.Items.FirstOrDefault(x => x.UserName == forUser && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                    if (_jsModuleRtc != null && con != null)
                    {
                        await _jsModuleRtc.InvokeVoidAsync("processAnswer", IpAddressUtilities.GetAuthority(forUrl), forUser, answer);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        [Description(DaprMessage.PubSubName)]
        public async Task SendCandidate(string forUrl, string forUser, Guid keyChatRoom, string candidate)
        {
            try
            {
                if (_jsModuleRtc != null && (ConnectList.FirstOrDefault(x => x.Key == keyChatRoom)?.Items.Any(i => i.UserName == forUser && IpAddressUtilities.CompareForAuthority(i.AuthorityUrl, forUrl)) ?? false))
                    await _jsModuleRtc.InvokeVoidAsync("processCandidate", IpAddressUtilities.GetAuthority(forUrl), forUser, candidate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task CloseP2P(string forUrl, string forUser, Guid keyChatRoom)
        {
            try
            {
                var conn = ConnectList.FirstOrDefault(y => y.Key == keyChatRoom);

                if (conn != null)
                {
                    var con = conn.Items.FirstOrDefault(x => x.UserName == forUser && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, forUrl));

                    if (con != null)
                    {
                        await CloseP2P(forUrl, forUser);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public Task RemoveNoReadMessage(Guid? keyChatRoom)
        {
            try
            {
                lock (NoReadMessages)
                {
                    if (keyChatRoom != null && NoReadMessages.ContainsKey(keyChatRoom.Value))
                    {
                        NoReadMessages.Remove(keyChatRoom.Value);
                        CallBackUpdateView?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.CompletedTask;
        }

        public int CountNoReadMessages(Guid keyChatRoom)
        {
            if (NoReadMessages.ContainsKey(keyChatRoom))
            {
                return NoReadMessages[keyChatRoom];
            }

            return 0;
        }

        public Task AddNoReadMessage(Guid keyChatRoom)
        {
            try
            {
                if (SelectConnect == null || SelectConnect.Key != keyChatRoom)
                {
                    lock (NoReadMessages)
                    {
                        if (NoReadMessages.ContainsKey(keyChatRoom))
                        {
                            NoReadMessages[keyChatRoom]++;
                        }
                        else
                        {
                            NoReadMessages.Add(keyChatRoom, 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.CompletedTask;
        }

        [JSInvokable]
        public async Task SendCandidateJs(string forUrl, string forUser, Guid keyChatRoom, string candidate)
        {
            try
            {
                var conn = ConnectList?.FirstOrDefault(x => x.Key == keyChatRoom && x.Items.Any(i => i.UserName == forUser && IpAddressUtilities.CompareForAuthority(i.AuthorityUrl, forUrl)));
                if (conn != null)
                {
                    await SendCoreHubAsync("SendCandidateForUrl", [forUrl, forUser, keyChatRoom, candidate]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [JSInvokable]
        public async Task DisconnectP2P(string forUrl, string forUser, Guid keyChatRoom)
        {
            try
            {
                await SendCoreHubAsync("DisconnectP2P", new object[] { forUrl, forUser, keyChatRoom });
                await CloseP2P(forUrl, forUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [JSInvokable]
        public async Task ConnectedP2P(string forUrl, string forUser, Guid keyChatRoom)
        {
            try
            {
                await SendCoreHubAsync("ConnectedP2P", [forUrl, forUser, keyChatRoom]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task ScrollToElement(bool? isCheckScroll = false)
        {
            try
            {
                await Task.Yield();
                if (isCheckScroll == true)
                {
                    var tBounding = await JSRun.InvokeAsync<BoundingClientRect?>("GetBoundingClientRectForQuery", ".pods-message-view");

                    var lBounding = await JSRun.InvokeAsync<BoundingClientRect?>("GetBoundingClientRectForQuery", ".message-container:last-child");

                    if (lBounding?.top > tBounding?.bottom)
                    {
                        //return;
                    }
                }
                if (CurrentChatMessages?.Count > 0)
                {
                    await JSRun.InvokeVoidAsync("ScrollToSelectElementQuery", ".message-container:last-child");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScrollToElement {ex.Message}");
            }
        }
        public async Task SetObjObserver()
        {
            try
            {
                if (_observer != null && AllCount > CurrentChatMessages?.Count)
                {
                    await Task.Delay(1000);
                    await _observer.InvokeVoidAsync("CreateObserver.startObserver", ".message-container", SelectConnect?.Key);
                }
            }
            catch (Exception ex)
            {
                Console.Write($"SetObjObserver {ex.Message}");
            }
        }

        public async Task ReloadLastPlayer(string fileName)
        {
            try
            {
                if (_SelectConnect != null)
                {
                    var name = Path.GetFileName(fileName.Replace(@"\", "/"));
                    var player = await JSRun.InvokeAsync<IJSObjectReference?>("document.querySelector", $":is(audio:has(source[src$='{name}']), video:has(source[src$='{name}']))");
                    player?.InvokeVoidAsync("load");
                    player?.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ReloadFirstPlayer {ex.Message}");
            }
        }

        public async Task AddCu(ContactInfo newCu)
        {
            try
            {
                await SendCoreHubAsync("AddOrUpdateContact", new[] { newCu });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task<bool> DeleteContactForUser(string? forUrl, string? forUser)
        {
            try
            {
                return await InvokeCoreHubAsync<bool>("DeleteContactForUser", new[] { forUrl, forUser });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return false;
        }

        async Task SendCoreHubAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_myHub.State != HubConnectionState.Connected)
                    await _myHub.StartAsync();
                await _myHub.SendCoreAsync(methodName, args, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{methodName}, {ex.Message}");
            }
        }

        async Task<T?> InvokeCoreHubAsync<T>(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_myHub.State != HubConnectionState.Connected)
                    await _myHub.StartAsync();
                return await _myHub.InvokeCoreAsync<T>(methodName, args, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{methodName}, {ex.Message}");
            }
            return default;
        }

        public async Task SaveNewConnect(ChatInfo connect)
        {
            try
            {
                await SendCoreHubAsync("SaveNewConnect", new[] { connect });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task SaveNewListConnect(IEnumerable<ChatInfo> connects)
        {
            try
            {
                await SendCoreHubAsync("SaveNewListConnect", new[] { connects });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task ChangeNameConnect(Guid keyChatRoom, string newName)
        {
            try
            {
                await SendCoreHubAsync("ChangeNameConnect", new object[] { keyChatRoom, newName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task DeleteChatRoom(Guid keyChatRoom)
        {
            try
            {
                await SendCoreHubAsync("DeleteChatRoom", new object[] { keyChatRoom });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task StartConnect(Guid keyChatRoom, TypeConnect typeConnect)
        {
            try
            {
                await SendCoreHubAsync("StartChatRoom", [keyChatRoom, typeConnect]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task CancelOutCall(Guid keyChatRoom)
        {
            try
            {
                await SendCoreHubAsync("CancelOutCallLocal", new object[] { keyChatRoom });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task CreateAnswer(Guid keyChatRoom, TypeConnect typeConnect)
        {
            try
            {
                await SendCoreHubAsync("ReadyCreateP2P", new object[] { keyChatRoom, typeConnect });
                await SetSelectValue(ConnectList.FirstOrDefault(x => x.Key == keyChatRoom));
                CallBackUpdateView?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task SendMessage(Guid keyChatRoom)
        {
            if (!string.IsNullOrWhiteSpace(TempMessage))
            {
                await SendCoreHubAsync("SendMessage", [keyChatRoom, TempMessage]);
            }
            TempMessage = string.Empty;
        }

        [JSInvokable]
        public async Task<string> StartRecord(Guid keyChatRoom)
        {
            var fileName = await InvokeCoreHubAsync<string?>("StartRecord", [keyChatRoom]) ?? string.Empty;
            if (!string.IsNullOrEmpty(fileName))
            {
                await OpenChanelForRecord(fileName);
                return fileName;
            }
            return string.Empty;
        }

        [JSInvokable]
        public async Task CallBackObserver(Guid? keyChatRoom)
        {
            try
            {
                await LoadMessageForChat(keyChatRoom);
                await SetObjObserver();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        async Task LoadMessageForChat(Guid? keyChatRoom)
        {
            try
            {
                var newData = await InvokeCoreHubAsync<MessagesAndAllCount?>("GetMessageForChat", [keyChatRoom, SkipData, TakeData, Any.Pack(FiltrModel).ToByteArray()]);

                CurrentChatMessages ??= new();

                if (newData != null)
                {
                    if (newData.Messages != null)
                    {
                        var addMessage = newData.Messages.Except(CurrentChatMessages).ToList();

                        CurrentChatMessages.InsertRange(0, addMessage);

                        if (newData.AllCount > CurrentChatMessages?.Count)
                        {
                            SkipData += newData.Messages.Count;
                        }
                    }
                    AllCount = newData.AllCount;
                }
                LoadChangeSelectValue = false;
                CallBackUpdateView?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        async Task InitMessageForChat(Guid? keyChatRoom)
        {
            try
            {
                if (keyChatRoom != null)
                {
                    await LoadMessageForChat(keyChatRoom);

                    await ScrollToElement();

                    await SetObjObserver();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error {name}, {message}", nameof(InitMessageForChat), ex.Message);
            }
        }

        public async Task SetFiltrModel(ViewPodsMessageFiltr filtr)
        {
            FiltrModel = filtr;
            await ResetChatMessage();
            if (SelectConnect != null)
            {
                await InitMessageForChat(SelectConnect.Key);
            }
        }

        public async Task SendAddItemsForChatRoom(Guid keyChatRoom, IEnumerable<ConnectInfo> itemsForAdd)
        {
            await SendCoreHubAsync("SendAddItemsForChatRoom", new object[] { keyChatRoom, itemsForAdd });
        }
        public async Task SendDeleteItemsForChatRoom(Guid keyChatRoom, IEnumerable<ConnectInfo> itemsForDelete)
        {
            await SendCoreHubAsync("SendDeleteItemsForChatRoom", new object[] { keyChatRoom, itemsForDelete });
        }

        public async Task CancelCallLocal(Guid keyChatRoom)
        {
            try
            {
                await SendCoreHubAsync("CancelCallLocal", new object[] { keyChatRoom });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task AddVideoToLocalStream(Guid keyChatRoom, TypeShare type)
        {
            try
            {
                var conn = ConnectList?.FirstOrDefault(x => x.Key == keyChatRoom);
                if (_jsModuleRtc != null && conn != null)
                {
                    var isOk = await _jsModuleRtc.InvokeAsync<bool?>("ChangeLocalStream", type.ToString());

                    if (isOk == true)
                    {
                        var typeOutCall = type switch
                        {
                            TypeShare.VideoOn => TypeConnect.Video,
                            TypeShare.ScreenOn => TypeConnect.Screen,
                            _ => TypeConnect.Sound
                        };
                        foreach (var forUrl in conn.Items.Where(x => x.State == StateCall.Connected))
                        {
                            var newOffer = await _jsModuleRtc.InvokeAsync<string?>("StartOfferForChangeStream", IpAddressUtilities.GetAuthority(forUrl.AuthorityUrl), forUrl.UserName, conn.Key);
                            if (!string.IsNullOrEmpty(newOffer))
                            {
                                await SendCoreHubAsync("SendChangeRemoteStream", [forUrl.AuthorityUrl, forUrl.UserName, conn.Key, newOffer, typeOutCall]);
                            }
                        }
                    }
                }
            }
            catch
            {
                _logger.LogError("Error add video");
            }
        }

        public async Task ChangeAudio(Guid keyChatRoom, bool isRecord)
        {
            try
            {
                var conn = ConnectList?.FirstOrDefault(x => x.Key == keyChatRoom);
                if (_jsModuleRtc == null || conn == null) return;
                await _jsModuleRtc.InvokeVoidAsync("ChangeSoundTrack", isRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public StateCall GetStateSelectCalling
        {
            get
            {
                return GetStateCalling(SelectConnect);
            }
        }

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
                else return StateCall.Disconnect;
            }
            return StateCall.Error;
        }

        async Task CloseP2P(string? url, string? forUser)
        {
            try
            {
                if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(forUser))
                {
                    if (_jsModuleRtc != null)
                    {
                        await _jsModuleRtc.InvokeVoidAsync("closePeerConnection", IpAddressUtilities.GetAuthority(url), forUser);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task CloseCallAction(Guid keyChatRoom)
        {
            try
            {
                var conn = ConnectList?.FirstOrDefault(x => x.Key == keyChatRoom);
                if (conn != null)
                {
                    await SendCoreHubAsync("CloseCallAction", [keyChatRoom]);
                    //Закрываем все P2P
                    foreach (var forUrl in conn.Items)
                    {
                        await CloseP2P(forUrl.AuthorityUrl, forUrl.UserName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task OpenChanelForRecord(string fileName)
        {
            channelForRecord?.Writer.TryComplete();
            channelForRecord = System.Threading.Channels.Channel.CreateUnbounded<byte[]>();
            await SendCoreHubAsync("UploadFile", [channelForRecord.Reader, fileName]);
        }

        public async IAsyncEnumerable<long> UploadFileToServer(Guid keyChatRoom, string message, string fileExt, Stream stream)
        {
            try
            {
                long UploadByte = 0;

                channelForUpload?.Writer.TryComplete();

                var pathToChat = await InvokeCoreHubAsync<string?>("CreatePathForChat", [keyChatRoom]) ?? string.Empty;

                var generatedName = Path.ChangeExtension(Path.GetRandomFileName(), fileExt);

                var fullPath = Path.Combine(pathToChat, generatedName);

                byte[] buffer = new byte[BufferSizeSignal];

                var readCount = 0;

                channelForUpload = System.Threading.Channels.Channel.CreateUnbounded<byte[]>();
                await SendCoreHubAsync("UploadFile", [channelForUpload.Reader, fullPath]);
                int countWhile = 0;
                while ((readCount = await stream.ReadAsync(buffer)) > 0)
                {
                    if (readCount > 0)
                    {
                        UploadByte += (readCount / 1024);
                        if (!channelForUpload.Writer.TryWrite(buffer.Take(readCount).ToArray()))
                        {
                            break;
                        }
                        countWhile++;
                        if (countWhile == 10)
                        {
                            countWhile = 0;
                            yield return UploadByte;
                            UploadByte = 0;
                        }
                    }
                }
                CallBackUpdateView?.Invoke();

                channelForUpload?.Writer.TryComplete();

                await SendCoreHubAsync("SendAllUsersFile", [keyChatRoom, message, fullPath]);

            }
            finally
            {
                _logger.LogTrace(@"Загрузка файла завершена, {fileName}", message);
            }
            yield break;
        }

        [JSInvokable]
        public async Task StopStreamToFile(string fileName)
        {
            if (channelForRecord != null)
            {
                await Task.Delay(1000);
                channelForRecord.Writer.TryComplete();
                try
                {
                    await ReloadLastPlayer(fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }

            }
        }

        [JSInvokable]
        public void StreamToFile(byte[]? btoa)
        {
            if (btoa != null)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (channelForRecord != null)
                        {
                            foreach (var item in btoa.Chunk(BufferSizeSignal))
                            {
                                if (!channelForRecord.Writer.TryWrite(item))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"StreamToFile {ex.Message}");
                    }
                });
            }
        }

        public async ValueTask CloseAllConnect()
        {
            _logger.LogTrace("Закрываем все открытые подключения {count}", ConnectList.Count(x => GetStateCalling(x) >= StateCall.Calling));
            foreach (var conn in ConnectList.Where(x => GetStateCalling(x) >= StateCall.Calling))
            {
                await CloseCallAction(conn.Key);
            }
        }

        public async ValueTask DisposeIndexPage()
        {
            NoReadMessages.Clear();
            await SetSelectValue(null);
            await CloseAllConnect();
            ConnectList.Clear();
            ContactList.Clear();
            await _myHub.StopAsync();
        }

        public async ValueTask DisposeModuleRtc()
        {
            if (_jsModuleRtc != null)
            {
                await _jsModuleRtc.InvokeVoidAsync("StopLocalStream");
                await _jsModuleRtc.DisposeAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAllConnect();
            await DisposeModuleRtc();
            _jsThis?.DisposeAsync();
            playerCaller?.DisposeAsync();
            _observer?.DisposeAsync();
            _push?.DisposeAsync();
            await _myHub.DisposeAsync();
        }

        public bool IsMyAuthorityUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return (IpAddressUtilities.CompareForHost(url, "localhost") || IpAddressUtilities.CompareForHost(url, "127.0.0.1") || IpAddressUtilities.CompareForHost(url, "0.0.0.0") || IpAddressUtilities.CompareForHost(url, MyAuthorityUrl));
        }

    }
}

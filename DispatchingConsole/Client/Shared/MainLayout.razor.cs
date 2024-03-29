﻿using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using static BlazorLibrary.Shared.Main;

namespace DispatchingConsole.Client.Shared
{
    partial class MainLayout : IAsyncDisposable
    {
        List<ChatInfo> ConnectList => _webRtc.ConnectList;

        bool LoadConnectList => _webRtc.LoadConnectList;

        public ChatInfo? SelectConnect
        {
            get
            {
                return _webRtc.SelectConnect;
            }
            set
            {
                _webRtc.SelectConnect = value;
            }
        }

        public List<ContactInfo>? SelectList = null;

        string NewName = string.Empty;
        string NewUser = string.Empty;
        int NewPort = 2291;
        string NewConnectName = string.Empty;
        string NewIp = string.Empty;

        int StaffId = 0;
        string? UserName
        {
            get
            {
                return _webRtc.UserName;
            }
            set
            {
                _webRtc.UserName = value;
            }
        }
        bool IsAdd = false;

        bool IsCreateConnect = false;
        bool IsEditConnect = false;

        string SearchValue = string.Empty;

        bool isDeleteChatRoom
        {
            get
            {
                return _webRtc.isDeleteChatRoom;
            }
            set
            {
                _webRtc.isDeleteChatRoom = value;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            _ = JSRuntime.InvokeVoidAsync("Notification.requestPermission");
            var reference = DotNetObjectReference.Create(this);
            _ = JSRuntime.InvokeVoidAsync("CloseWindows", reference);

            try
            {
                var _jsModele = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./js/registerEventsForUploadFile.js?v={AssemblyNames.GetVersionPKO}");
                await _jsModele.InvokeVoidAsync("registerEventsForUploadFile", "paste", "pastefiles");
                await _jsModele.InvokeVoidAsync("registerEventsForUploadFile", "drop", "dropfiles");
                await _jsModele.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(@"Ошибка загрузки дополнительных модулей, {Message}", ex.Message);
            }
        }

        IEnumerable<ContactInfo>? GetCuInfoList
        {
            get
            {
                return _webRtc.ContactList.Where(x => (UserName != x.UserName | x.Type != TypeContact.Local) && $"{x.Name} - {x.UserName}".Contains(SearchValue)).OrderBy(x => x.Type).ThenBy(x => x.AuthorityUrl).ThenBy(x => x.UserName);
            }
        }

        async void DeleteUser()
        {
            if (SelectList?.LastOrDefault() != null)
            {
                var last = SelectList.Last();
                var b = await _webRtc.DeleteContactForUser(last.AuthorityUrl, last.UserName);
                if (b)
                {
                    SelectList.Remove(last);
                    _webRtc.ContactList.Remove(last);
                }
                else
                {
                    MessageView?.AddError("", GsoRep["ERROR_DELETE"]);
                }
            }
        }


        public async Task LoadFirstLevel()
        {
            _webRtc.LoadConnectList = true;
            StaffId = await _User.GetLocalStaff();

            _webRtc.LoadConnectList = false;
        }

        void SetSelectList(List<ContactInfo>? items)
        {
            SelectList = items;
        }


        void SetSelectChat(List<ChatInfo>? items)
        {
            SelectConnect = items?.LastOrDefault();
        }

        void AddConnectInfo()
        {
            if (!string.IsNullOrEmpty(NewName) && !string.IsNullOrEmpty(NewUser) && !string.IsNullOrEmpty(NewIp) && NewPort > 0)
            {
                if (!_webRtc.ContactList.Any(x => x.UserName == NewUser && x.Name == NewName && IpAddressUtilities.CompareForAuthority($"{NewIp}:{NewPort}", x.AuthorityUrl)))
                    _ = _webRtc.AddCu(new ContactInfo(NewName, $"{NewIp}:{NewPort}", NewUser, 0));
            }
            CloseAddUser();
        }

        void CloseAddUser()
        {
            NewName = string.Empty;
            NewIp = string.Empty;
            NewUser = string.Empty;
            NewPort = 2291;
            IsAdd = false;
        }

        async Task CreateConnect()
        {
            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(NewConnectName) && SelectList?.Count > 0)
            {
                var listConnect = SelectList.ToList();
                if (!listConnect.Any(x => x.UserName == UserName && _webRtc.IsMyAuthorityUrl(x.AuthorityUrl)))
                    listConnect.Add(new ContactInfo(DispRep["YOU"], _webRtc.MyAuthorityUrl, UserName, StaffId));
                if (listConnect.Count > 0)
                {
                    var newConnect = new ChatInfo(NewConnectName, listConnect.Select(x => new ConnectInfo(x.AuthorityUrl, x.UserName)), IpAddressUtilities.GetAuthority(_webRtc.MyAuthorityUrl), UserName, false);
                    await _webRtc.SaveNewConnect(newConnect);
                    if (!ConnectList.Any(x => x.Key == newConnect.Key))
                        ConnectList.Add(newConnect);
                    SelectConnect = ConnectList.FirstOrDefault(x => x.Key == newConnect.Key);
                }
            }
            CloseCreateConnect();
        }

        void CloseCreateConnect()
        {
            SelectList = null;
            NewConnectName = string.Empty;
            IsCreateConnect = false;
        }
        void CloseEditConnect()
        {
            NewConnectName = string.Empty;
            IsEditConnect = false;
        }

        public void StartDeleteChatRoom()
        {
            if (SelectConnect == null) return;
            isDeleteChatRoom = true;
            StateHasChanged();
        }

        public async Task DeleteChatRoom()
        {
            if (SelectConnect == null) return;

            await _webRtc.DeleteChatRoom(SelectConnect.Key);

            ConnectList.RemoveAll(x => x.Key == SelectConnect.Key);
            _webRtc.CurrentChatMessages = new();
            SelectConnect = null;
            isDeleteChatRoom = false;
            _webRtc.IsEditItemsConnect = false;
        }

        void ViewEditModal()
        {
            if (SelectConnect != null && !IsCreateConnect)
            {
                NewConnectName = SelectConnect.NameRoom;
                IsEditConnect = true;
            }
            else
                CloseEditConnect();
        }



        void SaveNewNameConnect()
        {
            if (SelectConnect != null && !string.IsNullOrEmpty(NewConnectName))
            {
                _ = _webRtc.ChangeNameConnect(SelectConnect.Key, NewConnectName);
                if (ConnectList.Any(x => x.Key == SelectConnect.Key))
                {
                    var conn = ConnectList.First(x => x.Key == SelectConnect.Key);
                    conn.NameRoom = NewConnectName;
                }
            }
            CloseEditConnect();
        }

        [JSInvokable("CloseWindows")]
        public async ValueTask DisposeAsync()
        {
            SelectConnect = null;
            await _webRtc.DisposeAsync();
        }
    }
}

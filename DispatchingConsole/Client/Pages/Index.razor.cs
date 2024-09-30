using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using DispatchingConsole.Client.Shared;
using FiltersGSOProto.V1;
using LocalizationLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using static BlazorLibrary.Shared.Main;

namespace DispatchingConsole.Client.Pages
{
    partial class Index : IAsyncDisposable
    {
        [CascadingParameter]
        public MainLayout? Layout { get; set; }

        [Parameter]
        public string? MissedKeyChatRoom { get; set; }

        bool isSoundRecord = true;

        bool IsLoadPage => _webRtc.LoadConnectList || _webRtc.LoadChangeSelectValue;

        bool IsRecording { get; set; } = false;

        bool IsEditItemsConnect
        {
            get
            {
                return _webRtc.IsEditItemsConnect;
            }
            set
            {
                _webRtc.IsEditItemsConnect = value;
            }
        }

        List<ConnectInfo>? SelectItemConnect = null;
        List<ConnectInfo>? NewItemConnect = null;
        List<ContactInfo>? SelectListConnect = null;

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

        public bool _shouldPreventDefault = false;
        ChatInfo? SelectConnect
        {
            get
            {
                return _webRtc.SelectConnect;
            }
        }

        ElementReference _textArea { get; set; }

        long FileSize = 0;
        long FileSizeForDrop = 0;

        string? _appId { get; set; }

        private IJSObjectReference? _jsModuleRecordAudio;
        private IJSObjectReference? _recordAudio;

        int StartTimer = 0;

        Guid? RecordForChat = null;
        readonly System.Timers.Timer TimerRecord = new(1000);

        long UploadForDialog = 0;
        long UploadForDrop = 0;
        Guid? DropFileToChat = null;
        protected override async Task OnInitializedAsync()
        {
            _webRtc.CallBackUpdateView = StateHasChanged;
            _ = _webRtc.OnInitializedCommunicationAsync("localVideo", "remoteVideoArray");
            _appId = Http.DefaultRequestHeaders.GetHeader(nameof(CookieName.AppId));

            UserName = await _User.GetName() ?? "Error get ligin";

            await _webRtc.OnStartListenLocalHub(_appId);
            if (Layout != null)
            {
                await Layout.LoadFirstLevel();
            }
            await _webRtc.OnLoadMissedCall();
            TimerRecord.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            StartTimer--;
            if (StartTimer <= 0)
            {
                _ = SendStopRecordAudio();
                TimerRecord.Stop();
            }
            StateHasChanged();
        }

        string TimeLeft
        {
            get
            {
                var minutes = Math.Floor((double)(StartTimer / 60));
                var seconds = StartTimer - minutes * 60;
                return $"{minutes.ToString("00")}:{seconds.ToString("00")}";
            }
        }

        IEnumerable<ContactInfo>? GetCuInfoListForEdit
        {
            get
            {
                return _webRtc.ContactList.Where(x => (UserName != x.UserName | x.Type != TypeContact.Local) && (!NewItemConnect?.Select(n => $"{n.AuthorityUrl}&{n.UserName}").Contains($"{x.AuthorityUrl}&{x.UserName}") ?? true));
            }
        }

        void OnDragOver(DragEventArgs e)
        {
            e.DataTransfer.DropEffect = "move";
        }

        void CloseEditListConnect()
        {
            NewItemConnect = null;
            IsEditItemsConnect = false;
            SelectItemConnect = null;
            SelectListConnect = null;
        }
        void RemoveSelect()
        {
            if (NewItemConnect != null && SelectItemConnect?.Count > 0)
            {
                NewItemConnect.RemoveAll(x => SelectItemConnect.Where(s => x.UserName != UserName | !_webRtc.IsMyAuthorityUrl(s.AuthorityUrl)).Contains(x));
            }
            SelectItemConnect = null;
        }

        void SetSelectItemConnect(List<ConnectInfo>? items)
        {
            SelectItemConnect = items?.Where(x => x.UserName != UserName | !_webRtc.IsMyAuthorityUrl(x.AuthorityUrl)).ToList();
        }

        async Task SaveChangeItems()
        {
            if (SelectConnect != null && NewItemConnect?.Count > 1)
            {
                var deleteItems = SelectConnect.Items.ExceptBy(NewItemConnect.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), (x) => $"{x.AuthorityUrl}&{x.UserName}").ToList();
                var addItems = NewItemConnect.ExceptBy(SelectConnect.Items.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), (x) => $"{x.AuthorityUrl}&{x.UserName}").ToList();

                if (deleteItems.Count > 0)
                {
                    SelectConnect.Items.RemoveAll(x => deleteItems.Any(d => x.UserName == d.UserName && IpAddressUtilities.CompareForAuthority(d.AuthorityUrl, x.AuthorityUrl)));
                    await _webRtc.SendDeleteItemsForChatRoom(SelectConnect.Key, deleteItems);
                }
                if (addItems.Count > 0)
                {
                    SelectConnect.Items.AddRange(addItems);
                    await _webRtc.SendAddItemsForChatRoom(SelectConnect.Key, addItems);
                }
            }
            CloseEditListConnect();
        }

        void RemoveAll()
        {
            if (NewItemConnect != null)
            {
                NewItemConnect.RemoveAll(s => s.UserName != UserName | !_webRtc.IsMyAuthorityUrl(s.AuthorityUrl));
            }
        }

        void SetSelectListConnect(List<ContactInfo>? items)
        {
            SelectListConnect = items;
        }

        void ChangeItemsConnect()
        {
            if (SelectConnect == null) return;

            NewItemConnect = SelectConnect.Items.ToList();
            IsEditItemsConnect = true;
        }

        void AddSelect()
        {
            if (NewItemConnect != null && SelectListConnect?.Count > 0)
            {
                var addList = SelectListConnect.Select(x => new ConnectInfo(x.AuthorityUrl, x.UserName));

                NewItemConnect.AddRange(addList.ExceptBy(NewItemConnect.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), (x) => $"{x.AuthorityUrl}&{x.UserName}"));
            }
        }

        bool IsICreate => (SelectConnect != null && !SelectConnect.IsDefault && SelectConnect.UserCreate == UserName && _webRtc.IsMyAuthorityUrl(SelectConnect.AuthorityCreate));

        string? GetNameCU(string? url, string? userName)
        {
            string result = $"{url} - {userName}";
            if (userName == UserName && _webRtc.IsMyAuthorityUrl(url))
            {
                result = $"{DispRep["YOU"]} ({userName})";
            }
            else
            {
                var firstElem = _webRtc.ContactList.FirstOrDefault(x => x.UserName == userName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, url));
                if (firstElem != null)
                {
                    result = $"{firstElem.Name} - {firstElem.UserName}";
                }
            }
            return result;
        }

        bool GetStateUser(string? url, string? userName)
        {
            var firstElem = _webRtc.ContactList.FirstOrDefault(x => x.UserName == userName && IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, url));
            if (firstElem != null)
            {
                return firstElem.IsActive;
            }
            return false;
        }

        void StartDeleteChatRoom()
        {
            if (SelectConnect == null || Layout == null) return;
            Layout.StartDeleteChatRoom();
        }

        /// <summary>
        /// Создаем исходящий вызов
        /// </summary>
        /// <param name="typeConnect"></param>
        /// <returns></returns>
        private async Task StartConnect(TypeConnect typeConnect)
        {
            isSoundRecord = true;
            if (SelectConnect == null) return;
            await _webRtc.StartConnect(SelectConnect.Key, typeConnect);
        }


        /// <summary>
        /// Отменяем исходящий вызов
        /// </summary>
        /// <returns></returns>
        Task CancelOutCall()
        {
            if (SelectConnect != null)
            {
                _ = _webRtc.CancelOutCall(SelectConnect.Key);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Генерируем ответ
        /// </summary>
        /// <returns></returns>
        private async Task CreateAnswer(TypeConnect typeConnect)
        {
            isSoundRecord = true;

            if (_webRtc.InCallingConnect == null) return;

            await _webRtc.CreateAnswer(_webRtc.InCallingConnect.Key, typeConnect);
        }

        async Task SendMessageForClient()
        {
            if (SelectConnect == null) return;
            _ = _webRtc.SendMessage(SelectConnect.Key);

            await _textArea.FocusAsync();
        }

        async Task ShowOpenFilePicker()
        {
            try
            {
                if (SelectConnect == null) return;
                UploadForDialog = 0;
                var arrayFile = await JSRuntime.InvokeAsync<IJSObjectReference>("window.showOpenFilePicker", new { multiple = false });
                if (arrayFile != null)
                {
                    var firstFile = await arrayFile.InvokeAsync<IJSObjectReference>("shift");
                    var fileName = await JSRuntime.InvokeAsync<string>("Reflect.get", firstFile, "name");
                    var file = await firstFile.InvokeAsync<IJSStreamReference?>("getFile");

                    if (file != null)
                    {
                        if (file.Length > 50_000_000)
                        {
                            MessageView?.AddError("", $"{GsoRep["IDS_E_MAX_SIZE"]} ({50} {GsoRep[nameof(GsoLocalizationKey.MBYTE)]})");
                            return;
                        }
                        FileSize = file.Length;
                        using var s = await file.OpenReadStreamAsync(50_000_000);

                        await foreach (var number in _webRtc.UploadFileToServer(SelectConnect.Key, Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName), s))
                        {
                            UploadForDialog += number;
                            StateHasChanged();
                        }
                        UploadForDialog = 0;
                        await s.DisposeAsync();
                        await file.DisposeAsync();
                    }
                    await firstFile.DisposeAsync();
                    await arrayFile.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(@"Ошибка открытия окна выбора файла, {Message}", ex.Message);
            }
        }

        async Task OnSetFiles(OnSetFilesEventArgs e)
        {
            try
            {
                if (SelectConnect != null && DropFileToChat == null && !IsRecording)
                {
                    DropFileToChat = SelectConnect.Key;
                    UploadForDrop = 0;
                    if (e.Files?.Count > 0)
                    {
                        var files = e.Files.Where(x => x.Size <= 50_000_000);
                        if (files.Any())
                        {
                            FileSizeForDrop = files.Sum(x => x.Size);
                            foreach (var file in files)
                            {
                                try
                                {
                                    if (file.Stream != null)
                                    {
                                        using var s = await file.Stream.OpenReadStreamAsync(50_000_000);

                                        await foreach (var number in _webRtc.UploadFileToServer(DropFileToChat.Value, Path.GetFileNameWithoutExtension(file.Name) ?? "no message", Path.GetExtension(file.Name) ?? "", s))
                                        {
                                            UploadForDrop += number;
                                            StateHasChanged();
                                        }
                                        await s.DisposeAsync();
                                    }
                                }
                                catch (Exception eFile)
                                {
                                    _logger.LogError(@"Ошибка загрузки файла {name}, ошибка {message}", file.Name, eFile.Message);
                                }
                            }
                            UploadForDrop = 0;
                        }
                    }
                    DropFileToChat = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(@"Error onsetfiles, {message}", ex.Message);
            }

        }

        /// <summary>
        /// Отменяем входящий вызов (только для локального хаба и входящего подключения)
        /// </summary>
        async Task CancelCall()
        {
            if (_webRtc.InCallingConnect != null)
            {
                await _webRtc.CancelCallLocal(_webRtc.InCallingConnect.Key);
            }
        }

        /// <summary>
        /// Завершаем вызов, выходим из группы
        /// </summary>
        /// <returns></returns>
        private Task CloseCallAction()
        {
            if (SelectConnect != null)
            {
                return _webRtc.CloseCallAction(SelectConnect.Key);
            }
            return Task.CompletedTask;
        }

        async Task ChangeAudio(bool isRecord)
        {
            if (SelectConnect == null) return;
            isSoundRecord = isRecord;
            await _webRtc.ChangeAudio(SelectConnect.Key, isRecord);
        }

        async Task AddVideoToLocalStream(TypeShare type)
        {
            if (SelectConnect != null)
            {
                await _webRtc.AddVideoToLocalStream(SelectConnect.Key, type);
            }
        }

        async Task StartStreamRecordAudio()
        {
            try
            {
                StartTimer = 299;
                if (SelectConnect != null)
                {
                    RecordForChat = SelectConnect.Key;
                    if (_jsModuleRecordAudio == null)
                    {
                        _jsModuleRecordAudio = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./js/RecordAudio.js?v={AssemblyNames.GetVersionPKO}");
                    }
                    _recordAudio = await _jsModuleRecordAudio.InvokeAsync<IJSObjectReference>("recordAudio");

                    await _recordAudio.InvokeVoidAsync("start");
                    TimerRecord.Start();

                    IsRecording = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(@"Ошибка инициализации записи сообщения, {Message}", ex.Message);
            }
        }

        async Task SendStopRecordAudio()
        {
            try
            {
                TimerRecord.Stop();
                if (_recordAudio != null)
                {
                    var stream = await _recordAudio.InvokeAsync<IJSStreamReference>("stop");
                    if (RecordForChat != null)
                    {
                        if (stream.Length > 50_000_000)
                        {
                            MessageView?.AddError("", $"{GsoRep["IDS_E_MAX_SIZE"]} ({50} {GsoRep[nameof(GsoLocalizationKey.MBYTE)]})");
                        }
                        else
                        {
                            using var s = await stream.OpenReadStreamAsync(50_000_000);
                            await foreach (var number in _webRtc.UploadFileToServer(RecordForChat.Value, $"{UserName}: {DispRep["IDC_SOUNDMSG"]}", ".audio", s))
                            {

                            }
                        }
                    }
                    await stream.DisposeAsync();
                    await _recordAudio.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(@"Ошибка остановки записи сообщения, {Message}", ex.Message);
            }
            RecordForChat = null;
            IsRecording = false;
        }

        async Task SetSelectValue()
        {
            if (SelectConnect != null)
            {
                await _webRtc.SetSelectValue(_webRtc.ConnectList.FirstOrDefault(x => x.Key != SelectConnect.Key && (x.Items.Any(i => i.State >= StateCall.Calling))));
            }
        }
        async Task OnKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey && !e.AltKey && !e.CtrlKey && !e.MetaKey)
            {
                _shouldPreventDefault = true;
                await SendMessageForClient();
            }
            else
            {
                _shouldPreventDefault = false;
            }

        }

        string GetJoinIpAddress
        {
            get
            {
                string result = "";
                if (_webRtc.InCallingConnect != null)
                {
                    return string.Join("; ", _webRtc.InCallingConnect.Items.Select(x => $"{x.AuthorityUrl} - {x.UserName}"));
                }
                else if (SelectConnect != null)
                {
                    return string.Join("; ", SelectConnect.Items.Select(x => $"{x.AuthorityUrl} - {x.UserName}"));
                }
                return result;
            }
        }

        string GetNameRoom
        {
            get
            {
                string result = "";
                if (_webRtc.InCallingConnect != null)
                {
                    return _webRtc.InCallingConnect.NameRoom;
                }
                else if (SelectConnect != null)
                {
                    return SelectConnect.NameRoom;
                }
                return result;
            }
        }

        int GetCountUser
        {
            get
            {
                int result = 0;
                if (_webRtc.InCallingConnect != null)
                {
                    return _webRtc.InCallingConnect.Items.Count;
                }
                else if (SelectConnect != null)
                {
                    return SelectConnect.Items.Count;
                }
                return result;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _jsModuleRecordAudio?.DisposeAsync();
            _recordAudio?.DisposeAsync();
            TimerRecord.Close();
            await _webRtc.DisposeIndexPage();
        }
    }
}

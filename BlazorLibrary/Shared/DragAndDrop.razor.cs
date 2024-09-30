using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared
{
    partial class DragAndDrop : IAsyncDisposable
    {
        [Parameter]
        public string? UrlUpload { get; set; }

        [Parameter]
        public string[]? FilesExtensions { get; set; }

        [Parameter]
        public bool IsMultiple { get; set; } = false;

        [Parameter]
        public EventCallback<long> EventTotalProgress { get; set; }

        [Parameter]
        public EventCallback EventEndUpload { get; set; }
        [Parameter]
        public EventCallback<Tuple<string, int>> EventSetStatusCodeForUpload { get; set; }

        [Parameter]
        public EventCallback<long> EventSetFiles { get; set; }

        private List<FileUploadInfo>? filesForUpload = null;

        IJSObjectReference? _jsUploadLargeFile;
        DotNetObjectReference<DragAndDrop>? _jsThis;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _jsThis = DotNetObjectReference.Create(this);
                _jsUploadLargeFile = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/SensorM.GsoUi.BlazorLibrary/script/UploadLargeFile.js?v={AssemblyNames.GetVersionPKO}");
                await Task.Yield();
                await _jsUploadLargeFile.InvokeVoidAsync("RegisterDragAndDropForElemManualUpload", ".drag-container", _jsThis, nameof(FromDragSetFiles), FilesExtensions, IsMultiple ? 50 : 1);
                await _jsUploadLargeFile.InvokeVoidAsync("RegistrChangeValueForInput", ".upload-large-file", _jsThis, nameof(FromDragSetFiles), FilesExtensions, IsMultiple ? 50 : 1);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка инициализации компонента: {message}", ex.Message);
            }
        }


        public async Task StartUploadFile()
        {
            try
            {
                if (filesForUpload?.Count > 0 && _jsUploadLargeFile != null)
                {
                    await _jsUploadLargeFile.InvokeVoidAsync("UploadManualDragFiles", UrlUpload, _jsThis, nameof(UploadProgress), nameof(SetStatusCodeForUpload));
                    await CallBackEndUpload();
                    filesForUpload = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка запуска загрузки: {message}", ex.Message);
            }
        }

        [JSInvokable]
        public async Task SetStatusCodeForUpload(string fileName, int statusCode)
        {
            try
            {
                var first = filesForUpload?.First(x => x.Name == fileName);
                if (first != null)
                {
                    first.StatusCode = statusCode;
                    first.EndUpload = DateTime.Now;
                    StateHasChanged();
                }
                if (EventSetStatusCodeForUpload.HasDelegate)
                {
                    await EventSetStatusCodeForUpload.InvokeAsync(new(fileName, statusCode));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки уведомления о изменении статуса загрузки: {message}", ex.Message);
            }
        }

        [JSInvokable]
        public void UploadProgress(long? loaded, long? total, string? fileName)
        {
            try
            {
                if (loaded > 0 && (filesForUpload?.Any(x => x.Name == fileName) ?? false))
                {
                    var first = filesForUpload.First(x => x.Name == fileName);

                    if (first.Loaded == 0)
                    {
                        first.StartUpload = DateTime.Now;
                    }
                    first.Loaded = loaded.Value / 1024;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка пересчета прогресса: {message}", ex.Message);
            }
        }

        [JSInvokable]
        public async Task FromDragSetFiles(FileUploadInfo[]? files, bool? isHaveFile = true)
        {
            filesForUpload = null;
            try
            {
                if (isHaveFile == true && _jsUploadLargeFile != null)
                {
                    if (files?.Length > 0)
                    {
                        filesForUpload = files.ToList();
                    }
                    else
                    {
                        MessageView?.AddError("", GsoRep["IDS_E_SELECTFILE"]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка переноса файлов в input: {message}", ex.Message);
            }
            await CallBackEventSetFiles();
            StateHasChanged();
        }

        string GetStatusCodeColor(int? statusCode)
        {
            if (statusCode == null)
            {
                return "text-muted";
            }
            else if (statusCode >= 200 && statusCode <= 299)
            {
                return "text-success";
            }
            else if (statusCode == (int)System.Net.HttpStatusCode.Conflict)
            {
                return "text-warning";
            }
            else
            {
                return "text-danger";
            }
        }

        public async Task AbortUpload()
        {
            try
            {
                if (_jsUploadLargeFile != null && filesForUpload?.Count > 0)
                {
                    await _jsUploadLargeFile.InvokeVoidAsync("AbortAll");
                }
                filesForUpload = null;
                await CallBackEndUpload();
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка прерывания загрузки: {message}", ex.Message);
            }
        }

        async Task CallBackEventTotalProgress()
        {
            try
            {
                if (EventTotalProgress.HasDelegate)
                {
                    var totalProgress = filesForUpload?.Sum(x => x.Loaded);
                    await EventTotalProgress.InvokeAsync(totalProgress ?? 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки уведомления о изменении прогресса загрузки: {message}", ex.Message);
            }
        }

        async Task CallBackEventSetFiles()
        {
            try
            {
                if (EventSetFiles.HasDelegate)
                {
                    var totalSize = filesForUpload?.Sum(x => x.Size);
                    await EventSetFiles.InvokeAsync(totalSize ?? 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки уведомления о получении файлов для: {message}", ex.Message);
            }
        }

        async Task CallBackEndUpload()
        {
            try
            {
                if (EventEndUpload.HasDelegate)
                {
                    await EventEndUpload.InvokeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка отправки уведомления о завершении загрузки: {message}", ex.Message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_jsUploadLargeFile != null)
                {
                    await _jsUploadLargeFile.InvokeVoidAsync("AbortAll");
                    await _jsUploadLargeFile.DisposeAsync();
                }
                if (_jsThis != null)
                {
                    _jsThis.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка освобождения ресурсов: {message}", ex.Message);
            }
        }
    }
}

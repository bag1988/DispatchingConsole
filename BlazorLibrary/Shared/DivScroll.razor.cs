using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;

namespace BlazorLibrary.Shared
{
    partial class DivScroll : IAsyncDisposable
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? SetClass { get; set; }

        [Parameter]
        public double? Devision { get; set; } = 1; //разделить на

        private ElementReference div = default!;

        private string? MaxHeight = "auto";
        private string? MinHeight = "auto";

        IJSObjectReference? _jsModuleMutationObserver;

        IJSObjectReference? _observerDiv;

        DotNetObjectReference<DivScroll>? _jsThis;

        BoundingClientRect OldDivRect = new();
        protected override async Task OnInitializedAsync()
        {
            try
            {
                _jsThis = DotNetObjectReference.Create(this);
                _jsModuleMutationObserver = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/SensorM.GsoUi.BlazorLibrary/script/ResizeObserver.js?v={AssemblyNames.GetVersionPKO}");

                _observerDiv = await _jsModuleMutationObserver.InvokeAsync<IJSObjectReference>("CreateResizeObserver", _jsThis, nameof(RecalculateDiv));

                await Task.Yield();

                await _observerDiv.InvokeVoidAsync("start", div);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        Task CalcHeight(BoundingClientRect? divRect, int scrollHeight)
        {
            try
            {
                var calc = "";
                if (divRect != null)
                {
                    if (OldDivRect.top != divRect.top)
                    {
                        calc = $"100vh - {divRect.top + 10}px";

                        if (Devision != null && Devision > 1)
                        {
                            calc = $"({calc})/{Devision}";
                        }
                        MaxHeight = $"calc({calc.ToString().Replace(",", ".")})";
                    }

                    if (divRect.height <= 400 && scrollHeight > 400)
                    {
                        MinHeight = "400px";
                    }
                    else if (scrollHeight < 400)
                    {
                        MinHeight = $"{scrollHeight}px";
                    }
                    else
                    {
                        MinHeight = "auto";
                    }
                    OldDivRect = divRect;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.CompletedTask;
        }

        [JSInvokable]
        public async Task RecalculateDiv(BoundingClientRect? divRect, int? scrollHeight = 0)
        {
            try
            {
                await CalcHeight(divRect, scrollHeight ?? 0);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_observerDiv != null)
            {
                await _observerDiv.InvokeVoidAsync("stop", div);
                await _observerDiv.DisposeAsync();
            }
            if (_jsModuleMutationObserver != null)
            {
                await _jsModuleMutationObserver.DisposeAsync();
            }
            if (_jsThis != null)
            {
                _jsThis.Dispose();
            }
        }
    }
}

using BlazorLibrary.Models;
using BlazorLibrary.Shared.Audio;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;
using SharedLibrary.Utilities;

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

        [Parameter]
        public bool IsMaxHeigth { get; set; } = true;

        [Parameter]
        public bool? IsSticky { get; set; } = true;

        private ElementReference div = default!;

        private string? MaxHeigth = "calc((100vh - 92px)/2)";

        IJSObjectReference? _jsModuleMutationObserver;

        IJSObjectReference? _observerDiv;

        DotNetObjectReference<DivScroll>? _jsThis;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _jsThis = DotNetObjectReference.Create(this);
                _jsModuleMutationObserver = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/SensorM.GsoUi.BlazorLibrary/script/ResizeObserver.js?v={AssemblyNames.GetVersionPKO}");

                _observerDiv = await _jsModuleMutationObserver.InvokeAsync<IJSObjectReference>("CreateResizeObserver", _jsThis, nameof(RecalculateDiv));

                await Task.Yield();

                await _observerDiv.InvokeVoidAsync("start", div);

                //var d = await JSRuntime.InvokeAsync<BoundingClientRect?>("GetBoundingClientRect", div);
                //await CalcHeight(d);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        Task CalcHeight(BoundingClientRect? divRect)
        {
            try
            {
                var calc = "";
                if (divRect != null)
                {
                    calc = $"100vh - {divRect.top + 10}px";

                    if (Devision != null && Devision > 1)
                    {
                        calc = $"({calc})/{Devision}";
                    }

                    MaxHeigth = $"calc({calc.ToString().Replace(",", ".")})";
                }
                else
                    MaxHeigth = "calc(100vh - 92px)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.CompletedTask;
        }

        [JSInvokable]
        public async Task RecalculateDiv(BoundingClientRect? divRect)
        {
            try
            {
                await CalcHeight(divRect);
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

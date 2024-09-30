using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;

namespace BlazorLibrary.Shared
{
    partial class DivDynamicHeight : IAsyncDisposable
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? SetClass { get; set; }

        [Parameter]
        public string? QuerySelectorBottomElement { get; set; }

        [Parameter]
        public ElementReference? BottomElement { get; set; }


        private ElementReference div = default!;

        private string? MaxHeight = "calc(100vh - 92px)";

        IJSObjectReference? _jsModuleMutationObserver;

        IJSObjectReference? _observerDiv;

        DotNetObjectReference<DivDynamicHeight>? _jsThis;
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

        async Task CalcHeight(BoundingClientRect? divRect, int scrollHeight)
        {
            try
            {
                if (divRect != null)
                {
                    BoundingClientRect? bottomElem = new();

                    if (!string.IsNullOrEmpty(QuerySelectorBottomElement))
                    {
                        bottomElem = await JSRuntime.InvokeAsync<BoundingClientRect?>("GetBoundingClientRectForQuery", QuerySelectorBottomElement);
                    }
                    else if (BottomElement != null)
                    {
                        bottomElem = await JSRuntime.InvokeAsync<BoundingClientRect?>("GetBoundingClientRect", BottomElement.Value);
                    }

                    var calc = $"100vh - {divRect.top + 10 + (bottomElem?.height ?? 0)}px";

                    MaxHeight = $"calc({calc.ToString().Replace(",", ".")})";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
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

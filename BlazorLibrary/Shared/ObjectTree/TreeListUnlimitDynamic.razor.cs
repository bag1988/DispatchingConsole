
using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorLibrary.Shared.ObjectTree
{
    partial class TreeListUnlimitDynamic
    {
        [Parameter]
        public RenderFragment<object>? ContentView { get; set; }

        [Parameter]
        public IEnumerable<ChildItemsDynamic>? Items { get; set; } = null;

        [Parameter]
        public EventCallback DbClick { get; set; }

        [Parameter]
        public EventCallback<List<object>> SetSelectList { get; set; }

        [Parameter]
        public EventCallback<object> SetCurrentItem { get; set; }

        [Parameter]
        public List<object>? SelectList { get; set; }

        [Parameter]
        public bool IsSetFocus { get; set; } = true;

        public ElementReference? Elem { get; set; }

        public bool _shouldPreventDefault = false;

        ViewTreeItemDynamic ElemTree { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (IsSetFocus)
            {
                await Task.Yield();
                Elem?.FocusAsync();
            }
        }

        public void SetFocus()
        {
            Elem?.FocusAsync();
        }

        public async Task KeySet(KeyboardEventArgs e)
        {
            _shouldPreventDefault = false;
            if (e.Key == "Enter")
            {
                await DbCallback();
                return;
            }
            else if (e.Key == "ArrowUp" || e.Key == "ArrowDown")
            {
                if (Items == null)
                    return;
                _shouldPreventDefault = true;

                if (ElemTree != null)
                {
                    ElemTree.SetLevel(e);
                    _ = JSRuntime?.InvokeVoidAsync("ScrollToSelectElement", Elem, ".bg-select");
                }
            }
            else if (e.Key == "ArrowLeft" || e.Key == "ArrowRight")
            {
                _shouldPreventDefault = false;
                if (ElemTree != null)
                {
                    await ElemTree.ViewLevel(e);
                }

            }
        }


        public async Task DbCallback()
        {
            if (DbClick.HasDelegate)
            {
                if (SelectList != null)
                {
                    await DbClick.InvokeAsync(SelectList.LastOrDefault());
                }
                return;
            }
        }


        async Task SetSelectCallback(List<object>? item)
        {
            if (SetSelectList.HasDelegate)
                await SetSelectList.InvokeAsync(item);
            else
            {
                SelectList = item;
            }
        }
    }
}

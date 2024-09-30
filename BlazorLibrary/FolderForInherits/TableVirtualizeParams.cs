using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.FolderForInherits
{
    public class TableVirtualizeParams<TItem> : MultiSelectVirtualize<TItem>
    {
        [Parameter]
        public virtual RenderFragment<TItem>? Tbody { get; set; }

        [Parameter, AllowNull]
        public RenderFragment? TSticky { get; set; } = null;

        [Parameter]
        public int? MinWidth { get; set; } = 900;

        [Parameter]
        public double? Devision { get; set; } = 1; //разделить на

        [Parameter]
        public bool? IsSticky { get; set; } = true;
    }
}

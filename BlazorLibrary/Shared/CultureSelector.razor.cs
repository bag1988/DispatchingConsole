using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.Shared
{
    partial class CultureSelector
    {
        [Parameter]
        public EventCallback<string> ChangeLanguage { get; set; }

        private string Culture
        {
            get => CultureInfo.CurrentCulture.Name;
            set
            {
                if (CultureInfo.CurrentCulture.Name != value && ChangeLanguage.HasDelegate)
                {
                    ChangeLanguage.InvokeAsync(value);
                }
            }
        }
    }
}

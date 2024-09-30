using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SensorM.GsoCore.SharedLibrary;
using SharedLibrary;

namespace BlazorLibrary.Shared.NavLink
{
    partial class NavMenu : IAsyncDisposable
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public int? Width { get; set; } = 250;

        [Parameter]
        public EventCallback LogoutUser { get; set; }

        [Parameter]
        public EventCallback<string> ChangeLanguage { get; set; }

        [Parameter]
        public ProductVersion? PVersion { get; set; }

        [Parameter]
        public bool CollapseNavMenu { get; set; } = true;

        string? NavMenuCssClass => CollapseNavMenu ? "collapse" : null;

        private DateTime dateNow = DateTime.Now;


        readonly System.Timers.Timer timer = new(1000);

        private bool IsViewAbout = false;

        private ElementReference div = default!;

        protected override Task OnInitializedAsync()
        {
            try
            {
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return base.OnInitializedAsync();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                dateNow = DateTime.Now;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка обновления времени {message}", ex.Message);
            }
        }

        public async Task KeySet(KeyboardEventArgs e)
        {
            if (e.Key == "ArrowUp" || e.Key == "ArrowDown")
            {
                var index = e.Key == "ArrowUp" ? -1 : 1;
                await JSRuntime.InvokeVoidAsync("HotKeys.SetFocusLink", div, index);
            }
        }

        string? GetVersionUi
        {
            get
            {
                return AssemblyNames.GetVersionPKO;
            }
        }


        string GetCompanyName => PVersion?.CompanyName switch
        {
            "kae" => Rep["KAE_NAME"],
            "sensor" => Rep["Sensor"],
            "sensorm" => Rep["Sensor_M"],
            _ => string.Empty,
        };
        string GetCompanyTitleName => PVersion?.CompanyName switch
        {
            "kae" => Rep["PKO_NAME_KAE"],
            "sensor" => Rep["PKO_NAME_SENSOR"],
            "sensorm" => Rep["PKO_NAME_SENSORM"],
            _ => string.Empty,
        };


        string GetPo => PVersion?.CompanyName switch
        {
            "sensorm" => string.Empty,
            _ => Rep["PO_NAME"],
        };


        string GetPoName => PVersion?.CompanyName switch
        {
            "kae" => Rep["PO_NAME_KAE"],
            "sensor" => Rep["PO_NAME_SENSOR"],
            "sensorm" => Rep["PO_NAME_SENSORM"],
            _ => string.Empty,
        };

        private void ToggleNavMenu()
        {
            MyNavigationManager.NavigateTo(MyNavigationManager.Uri);
        }

        public ValueTask DisposeAsync()
        {
            lock (timer)
            {
                timer.Stop();
                timer.Dispose();
            }
            return ValueTask.CompletedTask;
        }
    }
}

using System.ComponentModel;
using System.Net.Http.Json;
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

        private DateTime dateNow = DateTime.Now;

        private bool collapseNavMenu = true;

        private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        readonly System.Timers.Timer timer = new(1000);

        private bool IsViewAbout = false;

        private ProductVersion? PVersion = null!;

        private ElementReference div = default!;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    timer.Elapsed += (sender, eventArgs) =>
                    {
                        dateNow = DateTime.Now;
                        StateHasChanged();
                    };
                    timer.Start();
                });
                await PVersionFull();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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

        private async Task Logout()
        {
            await AuthenticationService.Logout();
        }

        private async Task PVersionFull()
        {
            try
            {
                var result = await Http.PostAsync("api/v1/allow/PVersionFull", null);
                if (result.IsSuccessStatusCode)
                {
                    PVersion = await result.Content.ReadFromJsonAsync<ProductVersion>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        string? GetVersionUi
        {
            get
            {
                return AssemblyNames.GetVersionPKO;
            }
        }

        string GetCompanyName
        {
            get
            {
                if (PVersion == null)
                {
                    return "";
                }
                if (PVersion.CompanyName == "kae")
                    return Rep["KAE_NAME"];
                return Rep["SensorM"];
            }
        }
        string GetCompanyTitleName
        {
            get
            {
                if (PVersion == null)
                {
                    return "";
                }
                if (PVersion.CompanyName == "kae")
                    return Rep["PKO_NAME_KAE"];
                return Rep["PKO_NAME_SENSOR"];
            }
        }

        string GetPoName
        {
            get
            {
                if (PVersion == null)
                {
                    return "";
                }
                if (PVersion.CompanyName == "kae")
                    return Rep["PO_NAME_KAE"];
                return Rep["PO_NAME_SENSOR"];
            }
        }

        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using LocalizationLibrary;

namespace BlazorLibrary
{
    public class ContolChangeModel : IDisposable
    {
        readonly ILogger<ContolChangeModel> _logger;
        readonly NavigationManager MyNavigationManager;
        readonly IStringLocalizer<GSOLocalization> GsoRep;
        readonly IJSRuntime JSRuntime;
        public ContolChangeModel(ILogger<ContolChangeModel> logger, NavigationManager myNavigationManager, IStringLocalizer<GSOLocalization> gsoRep, IJSRuntime jSRuntime)
        {
            _logger = logger;
            MyNavigationManager = myNavigationManager;
            GsoRep = gsoRep;
            JSRuntime = jSRuntime;
        }

        bool IsSetEventClose = false;
        private IDisposable? registration;
        public bool ModelIsEquals = true;

        public async Task SetEventChangeModel(bool modelIsEquals)
        {
            ModelIsEquals = modelIsEquals;

            if (!modelIsEquals && !IsSetEventClose)
            {
                IsSetEventClose = true;
                await JSRuntime.InvokeVoidAsync("AddCloseWindowsEvent");
                registration = MyNavigationManager.RegisterLocationChangingHandler(OnLocationChanging);
            }
            else if (modelIsEquals && IsSetEventClose)
            {
                await RemoveCloseWindowsEvent();
            }
        }
        public async Task RemoveCloseWindowsEvent()
        {
            ModelIsEquals = true;
            registration?.Dispose();
            if (IsSetEventClose)
            {
                await JSRuntime.InvokeVoidAsync("RemoveCloseWindowsEvent");
            }
            IsSetEventClose = false;
        }
        private async ValueTask OnLocationChanging(LocationChangingContext context)
        {
            try
            {
                if (context.TargetLocation != MyNavigationManager.Uri)
                {
                    var result = await ViewWarningLossData();
                    if (!result)
                    {
                        context.PreventNavigation();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка перехода {error}", ex.Message);
            }
        }

        public async Task<bool> ViewWarningLossData()
        {
            return await JSRuntime.InvokeAsync<bool>("confirm", GsoRep["W_DATA_LOSS"].ToString());
        }

        public void Dispose()
        {
            _ = RemoveCloseWindowsEvent();
        }
    }
}

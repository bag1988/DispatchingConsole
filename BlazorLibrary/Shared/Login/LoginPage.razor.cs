using Microsoft.AspNetCore.Components;
using SensorM.GsoCore.SharedLibrary.PuSubModel;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Login;

partial class LoginPage : IDisposable
{
    [Parameter]
    public EventCallback<RequestLogin> ActinStartAuthorize { get; set; }

    [Parameter]
    public EventCallback StopAuthorize { get; set; }

    [Parameter]
    public StatusAuthorize? StateAuthorize { get; set; }

    [Parameter]
    public bool IsProcessing { get; set; }

    readonly RequestLogin loginRequest = new();

    readonly System.Timers.Timer timerLoadAccess = new(TimeSpan.FromSeconds(1));

    uint LoadSecond = Main.MinTimeoutLoad;

    string? InfoMessage
    {
        get
        {
            if (StateAuthorize != null)
            {
                var response = GsoRep[StateAuthorize.Value.ToString()].Value;
                if (StateAuthorize == StatusAuthorize.START_WAIT)
                {
                    if (LoadSecond == 10 && timerLoadAccess != null)
                    {
                        timerLoadAccess.Start();
                    }

                    response = $"{response} ({LoadSecond})";
                }
                return response;
            }
            return null;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        timerLoadAccess.Elapsed += TimerLoadAccess_Elapsed;
        loginRequest.User = await _localStorage.GetLastUserName() ?? "";
    }

    private void TimerLoadAccess_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (LoadSecond > 0)
        {
            LoadSecond--;
            StateHasChanged();
        }
        else if (timerLoadAccess != null)
        {
            timerLoadAccess.Stop();
        }
    }

    private async Task SetLogin()
    {
        LoadSecond = Main.MinTimeoutLoad;
        if (ActinStartAuthorize.HasDelegate)
        {
            await ActinStartAuthorize.InvokeAsync(loginRequest);
        }
    }

    public void Dispose()
    {
        if (timerLoadAccess != null)
        {
            timerLoadAccess.Stop();
            timerLoadAccess.Dispose();
        }
    }
}

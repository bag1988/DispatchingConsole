using BlazorLibrary.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Login;

partial class LoginPage
{
    private string? ErrorString;

    bool IsProcessing = false;

    readonly RequestLogin loginRequest = new();

    protected override async Task OnInitializedAsync()
    {
        loginRequest.User = await _localStorage.GetLastUserName() ?? "";
    }

    private async Task SetLogin()
    {
        if (string.IsNullOrEmpty(loginRequest.User))
        {
            ErrorString = Rep["EmptyLogin"];
            return;
        }
        ErrorString = null;
        IsProcessing = true;
        ErrorLoginUser result = await AuthenticationService.Login(loginRequest);
        
        if (result == ErrorLoginUser.NoFindUser)
        {
            ErrorString = GsoRep["NO_FIND_USER"];
        }
        else if (result == ErrorLoginUser.NoConnect)
        {
            ErrorString = GsoRep["NO_CONNECT"];
        }
        else if (result == ErrorLoginUser.NoAccess)
        {
            ErrorString = GsoRep["NO_ACCESS"];
        }
        IsProcessing = false;
    }
}

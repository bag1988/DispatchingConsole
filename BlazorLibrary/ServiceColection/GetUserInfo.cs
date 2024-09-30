using Microsoft.AspNetCore.Components.Authorization;
using SharedLibrary;
using SharedLibrary.Models;

namespace BlazorLibrary.ServiceColection;

public class GetUserInfo
{
    readonly AuthenticationStateProvider _authState;

    public GetUserInfo(AuthenticationStateProvider authState)
    {
        _authState = authState;
    }

    async Task<AuthorizUser?> GetUserAsync()
    {
        AuthorizUser? user = null;
        try
        {
            if ((await _authState.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated ?? false)
            {
                user = new((await _authState.GetAuthenticationStateAsync()).User.Claims);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return user;
    }

    public async Task<string?> GetName()
    {
        string? name = (await _authState.GetAuthenticationStateAsync()).User.Identity?.Name;

        return name;
    }

    public async Task<int> GetLocalStaff()
    {
        int staff = 0;

        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;
        int.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.LocalStaff))?.Value, out staff);

        return staff;
    }

    public async Task<int> GetUserSessId()
    {
        int UserSessID = 0;
        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;

        int.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.UserSessID))?.Value, out UserSessID);

        return UserSessID;
    }

    public async Task<PermisionsUser> GetAuthPerm()
    {
        PermisionsUser per = (await GetUserAsync())?.Permisions ?? new();
        return per;
    }

    public async Task<byte[]?> GetAuthPermForSubsystem(int systemId)
    {
        var Permisions = await GetAuthPerm();
        return systemId switch
        {
            SubsystemType.SUBSYST_ASO => Permisions.PerAccAso,
            SubsystemType.SUBSYST_SZS => Permisions.PerAccSzs,
            SubsystemType.SUBSYST_GSO_STAFF => Permisions.PerAccCu,
            SubsystemType.SUBSYST_P16x => Permisions.PerAccP16,
            SubsystemType.SUBSYST_Security => Permisions.PerAccSec,
            SubsystemType.SUBSYST_Setting => Permisions.PerAccFn,
            SubsystemType.SUBSYST_RDM => Permisions.PerAccRdm,
            _ => null
        };
    }

    public async Task<bool> CheckPermForSubsystem(int systemId, int[]? PosBit = null)
    {
        var per = await GetAuthPermForSubsystem(systemId);
        return CheckPermission.CheckBitPos(per, PosBit);        
    }

    public async Task<int> GetUserId()
    {
        int UserID = 0;
        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;

        int.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.UserID))?.Value, out UserID);

        return UserID;
    }

    public async Task<bool> GetCanStartStopNotify()
    {
        bool CanStartStopNotify = false;
        var c = (await _authState.GetAuthenticationStateAsync()).User.Claims;
        bool.TryParse(c?.FirstOrDefault(x => x.Type == nameof(AuthorizUser.CanStartStopNotify))?.Value, out CanStartStopNotify);

        return CanStartStopNotify;
    }
}

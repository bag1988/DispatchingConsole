using System.Security.Claims;
using System.Text;
using System.Text.Json;
using SMDataServiceProto.V1;

namespace SharedLibrary.Models
{
    public class AuthorizUser
    {
        public string? UserName { get; set; } = "";
        public int? UserID { get; set; } = 0;
        public int? UserSessID { get; set; } = 0;
        public int? LocalStaff { get; set; } = 0;
        public int? SubSystemID { get; set; } = 0;
        public bool? CanStartStopNotify { get; set; } = false;
        public PermisionsUser Permisions { get; set; } = new();

        public string GetBase64Permissions()
        {
            var s = JsonSerializer.SerializeToElement(Permisions).ToString();
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        public void SetPermissions(string? Base64)
        {
            if (string.IsNullOrEmpty(Base64))
                return;

            var bodyUser = Encoding.UTF8.GetString(Convert.FromBase64String(Base64));
            Permisions = JsonSerializer.Deserialize<PermisionsUser>(bodyUser) ?? new();
        }


        public AuthorizUser()
        {

        }

        public AuthorizUser(IEnumerable<Claim> UserClaims)
        {
            int.TryParse(UserClaims.FirstOrDefault(x => x.Type == nameof(UserID))?.Value, out int ID);
            int.TryParse(UserClaims.FirstOrDefault(x => x.Type == nameof(UserSessID))?.Value, out int SessID);
            int.TryParse(UserClaims.FirstOrDefault(x => x.Type == nameof(LocalStaff))?.Value, out int Staff);
            bool.TryParse(UserClaims.FirstOrDefault(x => x.Type == nameof(LocalStaff))?.Value, out bool _CanStartStopNotify);

            SetPermissions(UserClaims.FirstOrDefault(x => x.Type == nameof(Permisions))?.Value);

            CanStartStopNotify = _CanStartStopNotify;
            UserID = ID;
            UserSessID = SessID;
            LocalStaff = Staff;
            SubSystemID = SubsystemType.SUBSYST_ASO;
            UserName = UserClaims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        }


        public IEnumerable<Claim> ToClaims()
        {
            List<Claim> claims = new() {
                new Claim(ClaimTypes.Name, UserName??""),
                new Claim(nameof(LocalStaff), LocalStaff?.ToString() ?? ""),
                new Claim(nameof(UserID), UserID?.ToString() ?? ""),
                new Claim(nameof(UserSessID), UserSessID?.ToString() ?? ""),
                new Claim(nameof(CanStartStopNotify), CanStartStopNotify?.ToString() ?? "false"),
                new Claim(nameof(Permisions), GetBase64Permissions())
            };
            return claims;
        }


    }

    public class PermisionsUser
    {
        public byte[] PerAccFn { get; set; } = new byte[8];
        public byte[] PerAccAso { get; set; } = new byte[8];
        public byte[] PerAccSzs { get; set; } = new byte[8];
        public byte[] PerAccCu { get; set; } = new byte[8];
        public byte[] PerAccP16 { get; set; } = new byte[8];
        public byte[] PerAccRdm { get; set; } = new byte[8];
        public byte[] PerAccSec { get; set; } = new byte[8];
    }


    public class UserInfo
    {
        public OBJ_ID? OBJID { get; set; }
        public string? Login { get; set; }
        public string? Passw { get; set; }
        public int Status { get; set; }
        public PermisionsUser? Permisions { get; set; } = new();
        public bool? SuperVision { get; set; } = false;
    }



}

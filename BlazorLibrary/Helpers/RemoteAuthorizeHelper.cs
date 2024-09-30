using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Helpers
{
    public static class RemoteAuthorizeHelper
    {
        private static byte[] GetKey128 => Guid.Parse("170c1c88-28fb-4426-8d33-77e943e67556").ToByteArray();
        private static byte[] GetKey => GetKey128.Concat(GetKey128).ToArray();
        public static string CreateToken(string user, string password)
        {
            var utcNow = DateTime.UtcNow;
            Claim[] claims =
            [
            new Claim( ClaimTypes.Name , user),
            new Claim( ClaimTypes.Hash , password)
            ];
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(GetKey), "HS256");
            var token = new JwtSecurityToken("ARMOD.Server", "ARMOD.Client", claims, utcNow, utcNow.AddMinutes(5), signingCredentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static RequestLogin? ParseToken(string token)
        {
            var claims = JwtParser.ParseClaimsFromJwt(token);
            if (claims != null)
            {
                var user = claims.FindFirst(ClaimTypes.Name)?.Value;
                var password = claims.FindFirst(ClaimTypes.Hash)?.Value;
                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
                {
                    return new RequestLogin() { User = user, Password = password };
                }
            }
            return null;
        }
    }
}

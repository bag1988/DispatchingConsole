using System.Text;
using System.Text.Json;
using SharedLibrary.Models;

namespace SensorM.GsoCommon.ServerLibrary.Utilities
{
    public class UsersToken
    {
        readonly Dictionary<string, string> _Tokens = new();

        public bool AddTokenForIp(string key, UserCookie userCookie)
        {
            try
            {
                var json = JsonSerializer.Serialize(userCookie);

                var cookieValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

                if (_Tokens.ContainsKey(key))
                {
                    _Tokens[key] = cookieValue;
                }
                else
                {
                    _Tokens.Add(key, cookieValue);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void RemoveTokenForIp(string key)
        {
            if (_Tokens.ContainsKey(key))
            {
                _Tokens.Remove(key);
            }
        }

        public UserCookie? GetTokenForIp(string key)
        {
            UserCookie? userCookie = null;
            try
            {
                if (_Tokens.ContainsKey(key))
                {
                    var cookieValue = _Tokens[key];

                    if (!string.IsNullOrEmpty(cookieValue))
                    {
                        var json = Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue));

                        if (!string.IsNullOrEmpty(json))
                        {
                            userCookie = JsonSerializer.Deserialize<UserCookie>(json);
                        }
                    }
                }
            }
            catch
            {
                RemoveTokenForIp(key);
            }
            return userCookie;
        }
    }
}

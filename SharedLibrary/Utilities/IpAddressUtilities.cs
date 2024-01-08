using System.Net;
using System.Text.RegularExpressions;

namespace SharedLibrary.Utilities
{
    public static class IpAddressUtilities
    {
        public static uint StringToUint(string ipAddress)
        {
            if (IPEndPoint.TryParse(ipAddress, out IPEndPoint? iPEndPoint))
            {
                var address = iPEndPoint.Address;
                byte[] bytes = address.GetAddressBytes();
                // flip big-endian(network order) to little-endian
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                return BitConverter.ToUInt32(bytes, 0);
            }
            return 0;
        }

        public static string UintToIpString(uint connect)
        {
            if (connect > 0xFFFF)
            {
                byte[] bytes = BitConverter.GetBytes(connect);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                return $"{new IPAddress(bytes)}";
            }
            return string.Empty;
        }

        public static string UintToString(uint connect)
        {
            string response = connect.ToString();

            if (connect > 0xFFFF)
            {
                byte[] bytes = BitConverter.GetBytes(connect);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                response = $"TCP {new IPAddress(bytes)}";
            }
            else if (connect > 0xFF)
            {
                response = $"UDP {connect}";
            }
            else
            {
                response = $"COM {connect}";
            }

            return response;
        }

        public static void ParseEndPoint(string? EndPoint, out string? ipAddress, out int? Port)
        {
            ipAddress = null;
            Port = null;

            if (string.IsNullOrEmpty(EndPoint))
                return;

            IPEndPoint.TryParse(EndPoint, out IPEndPoint? e);
            ipAddress = e?.Address.ToString();
            Port = e?.Port > 0 ? e.Port : null;
        }

        public static string? ParseEndPoint(string? EndPoint)
        {
            if (string.IsNullOrEmpty(EndPoint))
                return null;

            IPEndPoint.TryParse(EndPoint, out IPEndPoint? e);
            return e?.Address.ToString();
        }


        public static bool CompareForAuthority(string? url1, string? url2)
        {
            if (!string.IsNullOrEmpty(url1) && !string.IsNullOrEmpty(url2))
            {
                var q1 = GetAuthority(url1);
                var q2 = GetAuthority(url2);
                return q1.Equals(q2);
            }

            return false;
        }
        public static bool CompareForHost(string? url1, string? url2)
        {
            if (!string.IsNullOrEmpty(url1) && !string.IsNullOrEmpty(url2))
            {
                var q1 = ParseUri(url1).Host;
                var q2 = ParseUri(url2).Host;
                return q1?.Equals(q2) ?? false;
            }

            return false;
        }

        public static string GetAuthority(string? url)
        {
            return ParseUri(url).Authority ?? string.Empty;
        }

        public static string GetHost(string? url)
        {
            return ParseUri(url).Host ?? string.Empty;
        }


        public static UriParts ParseUri(string? uri)
        {
            string pattern = @"^(?:(http(?:s?))(?:://))?(((?:\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3})|(?:localhost))(?::(\d{1,5}))?)";

            UriParts parts = new();

            if (!string.IsNullOrEmpty(uri))
            {
                var maths = Regex.Match(uri, pattern);

                if (maths.Success)
                {
                    parts.Sheme = maths.Groups.GetValueOrDefault("1")?.Value;
                    parts.Authority = maths.Groups.GetValueOrDefault("2")?.Value;
                    parts.Host = maths.Groups.GetValueOrDefault("3")?.Value;
                    int.TryParse(maths.Groups.GetValueOrDefault("4")?.Value, out int port);
                    parts.Port = port;
                }
            }
            return parts;

        }

    }

    public class UriParts
    {
        public string? Sheme { get; set; }
        public string? Authority { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }

    }
}

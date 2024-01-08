using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorM.GsoCommon.ServerLibrary.Extensions
{
    public static class ParseUserAgent
    {
        public static string GetBrowserNameWithVersion(string? userAgent)
        {
            var browserWithVersion = "Other Browser";

            if(!string.IsNullOrEmpty(userAgent))
            {
                if (userAgent.IndexOf("Edg/") > -1)
                {
                    //Edge
                    browserWithVersion = "Edge";
                }
                else if (userAgent.IndexOf("Chromium/") > -1)
                {
                    //Chrome
                    browserWithVersion = "Chromium";
                }
                else if (userAgent.IndexOf("Chrome/") > -1)
                {
                    //Chrome
                    browserWithVersion = "Chrome";
                }
                else if (userAgent.IndexOf("Safari/") > -1)
                {
                    //Safari
                    browserWithVersion = "Safari";
                }
                else if (userAgent.IndexOf("Firefox/") > -1)
                {
                    //Firefox
                    browserWithVersion = "Firefox";
                }
                else if (userAgent.IndexOf("rv") > -1)
                {
                    //IE11
                    browserWithVersion = "Internet Explorer";
                }
                else if (userAgent.IndexOf("MSIE") > -1)
                {
                    //IE6-10
                    browserWithVersion = "Internet Explorer";
                }
            }

            return browserWithVersion;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorLibrary.Helpers
{
    public static class ParseUrlSegments
    {
        public static string[] GetSegments(string url)
        {
            return PathAndQuery(url).Split(['/']).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        public static bool IsHaveSegment(string url, string findSegment)
        {
            var arr = GetSegments(url).ToList();
            return arr.Contains(findSegment);
        }

        public static string RemoveSegment(string url, string removeSegment)
        {
            var arr = GetSegments(url).ToList();
            arr.Remove(removeSegment);
            var newUrl = string.Join("/", arr);
            return newUrl;
        }

        public static string AddSegment(string url, string addSegment)
        {
            var arr = GetSegments(url).ToList();
            arr.Add(addSegment);
            var newUrl = string.Join("/", arr);
            return newUrl;
        }

        public static int GetSystemId(string url)
        {
            var arr = GetSegments(url);
            int systemId;
            if (arr.Length > 1 || (arr.Length == 1 && int.TryParse(arr[0], out systemId)))
            {
                if (int.TryParse(arr[0], out systemId))
                {
                    return systemId;
                }
            }
            return 0;
        }
        public static string PathAndQuery(string url)
        {
            var s = new Uri(url);
            return s.PathAndQuery;
        }

        public static string AbsolutePath(string url)
        {
            var s = new Uri(url);
            return s.AbsolutePath;
        }

        public static string ReplaceSystemId(string url, int newId)
        {
            var path = PathAndQuery(url);
            var arr = GetSegments(url);
            int systemId;
            if (arr.Length > 1 || (arr.Length == 1 && int.TryParse(arr[0], out systemId)))
            {
                if (int.TryParse(arr[0], out systemId))
                {
                    path = $"/{newId}/{string.Join("/", arr.Skip(1))}";
                }
            }
            else if (newId > 0)
            {
                path = $"/{newId}/{string.Join("/", arr)}";
            }

            return path;
        }
    }
}

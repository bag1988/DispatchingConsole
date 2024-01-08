using System.Globalization;

namespace SharedLibrary
{
    public class SupportedCultures
    {
        public static CultureInfo[] Array
        {
            get => new CultureInfo[]{
                new CultureInfo("ru-RU")
            };
        }
    }
}

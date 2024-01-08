using System.Reflection;

namespace SensorM.GsoCore.SharedLibrary
{
    public static class AssemblyNames
    {
        public static string? GetVersionPKO => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        public static string? GetCompanyName => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

        public static string? GetProductName => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product;

        public static string? GetVersionExtensions => Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "VersionExtension")?.Value;
    }
}

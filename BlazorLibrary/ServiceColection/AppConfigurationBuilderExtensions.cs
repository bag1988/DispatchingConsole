using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorLibrary.ServiceColection
{
    public static class AppConfigurationBuilderExtensions
    {
        public static async Task AppLoggingConfiguration(this WebAssemblyHostBuilder builder)
        {
            try
            {
                var client = new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
                using var result = await client.PostAsync("api/v1/allow/GetLoggingSetting", null);
                Dictionary<string, string> keyValuePairs = new();
                if (result.IsSuccessStatusCode)
                {
                    keyValuePairs = await result.Content.ReadFromJsonAsync<Dictionary<string, string>?>() ?? new();      
                }
                
                builder.Services.AddSingleton(typeof(ILogger<>), typeof(LoggerInterceptor<>));
                builder.Services.AddLogging(logging =>
                {
                    if (keyValuePairs.ContainsKey("Default") && Enum.TryParse<LogLevel>(keyValuePairs["Default"], out var minimumLevel))
                    {
                        logging.SetMinimumLevel(minimumLevel);
                    }
                    else
                    {
                        logging.SetMinimumLevel(LogLevel.Error);
                    }
                    foreach (var item in keyValuePairs)
                    {
                        if (Enum.TryParse<LogLevel>(item.Value, out var level))
                        {
                            logging.AddFilter(item.Key, level);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

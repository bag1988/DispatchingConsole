using System.Globalization;
using System.Security.Claims;
using BlazorLibrary.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using SharedLibrary.GlobalEnums;
using SharedLibrary;
using BlazorLibrary.GlobalEnums;

namespace BlazorLibrary.ServiceColection
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddServiceBlazor(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddLocalization();

            services.AddHttpClient("WebAPI", (service, client) =>
            {
                client.BaseAddress = new Uri(service.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress);
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            services.AddTransient<LocalStorage>();

            services.AddAuthorizationCore(x =>
            {
                x.AddPolicy("Bearer", policy =>
                {
                    policy.AddAuthenticationSchemes("Bearer");
                    policy.RequireClaim(ClaimTypes.Name);
                });
            });

            services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

            services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebAPI"));

            services.AddTransient<GetUserInfo>();
            services.AddTransient<HubContextCreate>();
            services.AddTransient<OtherInfoForReport>();
            return services;
        }

        public static async Task SetHeaderAndRunAsync(this WebAssemblyHost host)
        {
            await host.Services.SetDefaultHeaderHttpClient();
            await host.RunAsync();
        }

        static async Task SetDefaultHeaderHttpClient(this IServiceProvider provider)
        {
            var culture = new CultureInfo("ru-RU");

            var js = provider.GetService<IJSRuntime>();
            try
            {
                if (js != null)
                {
                    var result = await js.InvokeAsync<string>("getCultureGlobal");

                    if (result != null && SupportLanguage.Get.Contains(result))
                    {
                        culture = new CultureInfo(result);
                    }
                    else if (SupportLanguage.Get.Contains(CultureInfo.CurrentUICulture.Name))
                    {
                        culture = CultureInfo.CurrentUICulture;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var _http = provider.GetService<HttpClient>();
            if (_http != null)
            {
                _http.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue(culture.Name));

                _http.DefaultRequestHeaders.AddHeader(MetaDataName.TimeZone, TimeZoneInfo.Local.Id);
                _http.DefaultRequestHeaders.Date = DateTimeOffset.Now;
                try
                {
                    if (js != null)
                    {
                        var appId = await js.InvokeAsync<string>("localStorage.getItem", CookieName.AppId);
                        if (string.IsNullOrEmpty(appId))
                        {
                            appId = Guid.NewGuid().ToString();

                            await js.InvokeVoidAsync("localStorage.setItem", CookieName.AppId, appId);
                        }
                        _http.DefaultRequestHeaders.AddHeader(nameof(CookieName.AppId), appId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

    }
}

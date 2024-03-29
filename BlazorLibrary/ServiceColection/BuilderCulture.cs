﻿using System.Globalization;
using BlazorLibrary.Helpers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using static System.Net.WebRequestMethods;

namespace BlazorLibrary.ServiceColection
{
    public class BuilderCulture
    {
        public static async Task Set(WebAssemblyHost host)
        {
            CultureInfo culture;
            var js = host.Services.GetRequiredService<IJSRuntime>();
            var Http = host.Services.GetRequiredService<HttpClient>();
            var result = await js.InvokeAsync<string>("getCultureGlobal");

            if (result != null)
            {
                culture = new CultureInfo(result);
            }
            else
            {
                culture = new CultureInfo("ru-RU");
                await js.InvokeVoidAsync("setCultureGlobal", "ru-RU");
            }

            if (Http != null)
            {
                Http.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue(culture.Name));
                Http.DefaultRequestHeaders.AddHeader(MetaDataName.TimeZone, TimeZoneInfo.Local.Id);
                Http.DefaultRequestHeaders.Date = DateTimeOffset.Now;

                var appId = await js.InvokeAsync<string?>("localStorage.getItem", CookieName.AppId);
                if (!string.IsNullOrEmpty(appId))
                {
                    Http.DefaultRequestHeaders.AddHeader(nameof(CookieName.AppId), appId);
                }
            }

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}

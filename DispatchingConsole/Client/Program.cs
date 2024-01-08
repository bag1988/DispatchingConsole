using BlazorLibrary.ServiceColection;
using DispatchingConsole.Client;
using DispatchingConsole.Client.WebRTC;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//HttpClient, InfoUser, ReplaceDictionary
builder.Services.AddServiceBlazor();
builder.Services.AddSingleton<CommunicationService>();
await builder.AppLoggingConfiguration();

var host = builder.Build();

await BuilderCulture.Set(host);

await host.RunAsync();

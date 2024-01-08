using ServiceLibrary;
using ServerLibrary;
using ServerLibrary.HubsProvider;
using SharedSettings.Extensions;
using ServiceLibrary.Logging.SeriLog;
using Serilog;
using ServiceLibrary.Diagnostic;
using SensorM.GsoCommon.ServerLibrary.Services;
using SensorM.GsoCommon.ServerLibrary.Context;
using Microsoft.AspNetCore.SignalR;
using DispatchingConsole.Server.BackgroundServices;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

LogExtensions.Initialize();

try
{
    LogExtensions.StartApplication();

    var builder = WebApplication.CreateBuilder(args);

    builder.ConfigureSharedServerConfiguration();

    builder.Services.AddOptions();

    builder.TryAddTracing<Program>(Tracing.Sources);

    builder.TryAddMetrics<Program>(Tracing.Sources);

    //Subscribe
    builder.Services.AddSubscribeNotify();

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    builder.Services.AddSignalRNotify();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddGrpc();
    builder.Services.AddGrpcClient<PodsProto.V1.PodsService.PodsServiceClient>(o => o.Address = new Uri($"http://dispatchingconsole.server:{(builder.Configuration.GetValue<int?>("DISPATCHINGCONSOLE_APP_GRPC_PORT") ?? 2491)}"));
    builder.Services.AddServerCollection();
    builder.Services.AddSMDataServices();

    builder.Services.AddCors(o => o.AddPolicy("AllowAll", corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    }));

    var dbPath = builder.Configuration.GetValue<string>("PodsBasePath") ?? "PodsBasePath/pods.db";
    var dbDir = Path.GetDirectoryName(dbPath) ?? "PodsBasePath";
    if (!Directory.Exists(dbDir))
    {
        Directory.CreateDirectory(dbDir);
    }
    builder.Services.AddSqlite<PodsContext>($"Filename={dbPath}", b => b.MigrationsAssembly("DispatchingConsole.Server"));

    builder.Services.AddHostedService<UpdateLocalUsers>();

    builder.Host.UseSerilog();
    var app = builder.Build();


    using (var scope = app.Services.CreateScope())
    {
        using (var db = scope.ServiceProvider.GetRequiredService<PodsContext>())
        {
            if (!db.Database.EnsureCreated())
            {
                var penMigration = db.Database.GetPendingMigrations();
                if (penMigration.Any())
                {
                    db.Database.Migrate();
                }
            }
            else
            {
                var historyCreator = db.Database.GetService<IHistoryRepository>();
                var b = historyCreator.Exists();
                if (!b)
                {
                    var sql = historyCreator.GetCreateScript();
                    db.Database.ExecuteSqlRaw(sql);
                    var insertSql = historyCreator.GetInsertScript(new HistoryRow("20240108094047_changeBase", "8.0.0"));
                    db.Database.ExecuteSqlRaw(insertSql);
                }
            }
        }
    }
    app.UseCors("AllowAll");


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    //app.UseHttpsRedirection();
    app.UseRequestLocalization();

    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles(new StaticFileOptions()
    {
        ServeUnknownFileTypes = true
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseGrpcWeb();

    //Dapr PubSub
    app.UseCloudEvents();

    app.MapRazorPages();
    app.MapControllers();

    //Dapr PubSub
    app.MapSubscribeHandler();

    app.MapHub<SharedHub>("/CommunicationHub");
    app.MapHub<ChatHub>("/CommunicationChatHub");

    app.MapGrpcService<PodsServiceV1>();

    app.MapFallbackToFile("_content/SensorM.GsoUi.BlazorLibrary/index.html");

    app.TryUseOpenTelemetryPrometheusScrapingEndpoint();

    app.Lifetime.ApplicationStopping.Register(LifeTimeStopping.SendAllLogout, app.Services.GetService<IHubContext<SharedHub>>());

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

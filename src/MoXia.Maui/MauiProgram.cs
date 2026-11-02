using CommunityToolkit.Maui;
using MauiBlazorToolkit.Extensions;
using Microsoft.AspNetCore.Components.WebView.Maui;
using MoXia.Control;
using MoXia.Control.Extensions;
using MoXia.Maui.Essentials;
using MoXia.Maui.Extensions;
using MoXia.Rcl.Extensions;

namespace MoXia.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiBlazorToolkit(options =>
                {
                    options.TitleBar = true;
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .ConfigureEssentials(essentials =>
                {
                    essentials.UseVersionTracking()
#if !WINDOWS
                              .OnAppAction(AppActionsHelper.HandleAppActions)
#endif
                              ;
                });

            builder.Services.AddMauiBlazorWebView();

#if IOS || MACCATALYST
            BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping(nameof(IBlazorWebView.HostPage), (handler, view) =>
            {
                handler.PlatformView.NavigationDelegate = new WebViewNavigationDelegate();
            });
            BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping(nameof(IBlazorWebView.RootComponents), (handler, view) =>
            {
                handler.PlatformView.NavigationDelegate = new WebViewNavigationDelegate();
            });
#endif

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            builder.Services.AddMasaBlazorConfig();

            builder.Services.AddSerilogConfig(SQLiteConstants.LogDatabasePath);

            builder.Services.AddSqlSugarConfig(SQLiteConstants.ConnectionString, SQLiteConstants.PrivacyConnectionString, SQLiteConstants.LogConnectionString);

            builder.Services.AddDependencyInjection();
            builder.Services.AddMcpControl();

            builder.Services.AddMauiExceptionHandler();

            var app = builder.Build();
#if WINDOWS || MACCATALYST
            app.Services.GetRequiredService<McpControlServer>()
                .StartAsync()
                .GetAwaiter()
                .GetResult();
#endif
            return app;
        }
    }
}

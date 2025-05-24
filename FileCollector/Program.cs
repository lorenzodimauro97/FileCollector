using System;
using FileCollector.Services;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using FileCollector.Services.Settings;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace FileCollector;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        PhotinoWindow? mainWindowInstance = null;

        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging(configure => configure
            .AddConsole()
            .SetMinimumLevel(LogLevel.Trace)
            .AddFilter("Photino.NET", LogLevel.Error)
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("Microsoft.AspNetCore.Components.RenderTree", LogLevel.Information)
        );

        appBuilder.Services.AddSingleton<SettingsService>();
        appBuilder.Services.AddSingleton<IoService>();
        appBuilder.Services.AddSingleton<GitIgnoreFilterService>();
        appBuilder.Services.AddSingleton<NavigationStateService>();
        appBuilder.Services.AddSingleton<FileTreeService>();
        appBuilder.Services.AddSingleton<ContentMergingService>();

        appBuilder.Services.AddSingleton<Func<PhotinoWindow>>(() => mainWindowInstance!);

        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();

        mainWindowInstance = app.MainWindow;

        app.MainWindow
            .SetIconFile("icon.ico")
            .SetTitle("File Collector")
            .SetLogVerbosity(0);

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            Console.WriteLine($"FATAL EXCEPTION: {error.ExceptionObject}");

            var userMessage = "A fatal error occurred. Please check logs or contact support.";
            if (error.ExceptionObject is Exception ex)
            {
                userMessage += $"\n\nDetails: {ex.Message}";
            }

            app?.MainWindow?.ShowMessage("Fatal Exception", userMessage);
        };

        app.Run();
    }
}
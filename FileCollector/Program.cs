using System;
using FileCollector.Services;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using FileCollector.Services.Settings;
using Microsoft.Extensions.Logging;
using Photino.NET; // Added for PhotinoWindow

namespace FileCollector;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        PhotinoWindow? mainWindowInstance = null; // To hold the instance once created

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

        // Register a factory/provider for PhotinoWindow
        // Components can inject Func<PhotinoWindow> and call it to get the instance.
        // The '!' asserts mainWindowInstance will be non-null when the Func is invoked by a component.
        appBuilder.Services.AddSingleton<Func<PhotinoWindow>>(() => mainWindowInstance!);

        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();

        mainWindowInstance = app.MainWindow; // Assign the actual instance

        app.MainWindow
            .SetIconFile("favicon.ico")
            .SetTitle("File Collector")
            .SetLogVerbosity(0);

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            Console.WriteLine($"FATAL EXCEPTION: {error.ExceptionObject}");
            // Log the full exception if possible using the configured logger, though it might be too late.
            // Logger?.LogError(error.ExceptionObject as Exception, "Unhandled application exception.");

            var userMessage = "A fatal error occurred. Please check logs or contact support.";
            if (error.ExceptionObject is Exception ex)
            {
                userMessage += $"\n\nDetails: {ex.Message}";
            }
            // Ensure app.MainWindow is not null before trying to ShowMessage
            app?.MainWindow?.ShowMessage("Fatal Exception", userMessage);
        };

        app.Run();
    }
}
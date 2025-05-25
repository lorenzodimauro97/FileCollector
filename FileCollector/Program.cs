using System;
using System.IO;
using FileCollector.Services;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using FileCollector.Services.Settings;
using Microsoft.Extensions.Logging;
using Photino.NET;
using Serilog;
using Serilog.Events;

namespace FileCollector;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose() 
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Components.RenderTree", LogEventLevel.Information)
            .MinimumLevel.Override("Photino.NET", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information 
            )
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "FileCollector-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 7,
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(5)
            )
            .CreateLogger();

        try
        {
            Log.Information("Starting File Collector application");

            PhotinoWindow? mainWindowInstance = null;

            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders(); 
                loggingBuilder.AddSerilog(dispose: true);
            });
            
            appBuilder.Services.AddHttpClient();

            appBuilder.Services.AddSingleton<SettingsService>();
            appBuilder.Services.AddSingleton<IoService>();
            appBuilder.Services.AddSingleton<GitIgnoreFilterService>();
            appBuilder.Services.AddSingleton<NavigationStateService>();
            appBuilder.Services.AddSingleton<FileTreeService>();
            appBuilder.Services.AddSingleton<ContentMergingService>();
            
            appBuilder.Services.AddSingleton<UpdateStateService>();
            appBuilder.Services.AddSingleton<UpdateService>();


            appBuilder.Services.AddSingleton<Func<PhotinoWindow>>(() => mainWindowInstance!);

            appBuilder.RootComponents.Add<App>("app");

            var app = appBuilder.Build();

            mainWindowInstance = app.MainWindow;

            app.MainWindow
                .SetIconFile("icon.ico")
                .SetTitle("File Collector")
                .SetLogVerbosity(2); 

            AppDomain.CurrentDomain.UnhandledException += (_, error) =>
            {
                Log.Fatal(error.ExceptionObject as Exception, "Unhandled exception");
                
                var userMessage = "A fatal error occurred. Please check logs or contact support.";
                if (error.ExceptionObject is Exception ex)
                {
                    userMessage += $"\n\nDetails: {ex.Message}";
                }
                app.MainWindow?.ShowMessage("Fatal Exception", userMessage);
            };

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.Information("Shutting down File Collector application");
            Log.CloseAndFlush();
        }
    }
}
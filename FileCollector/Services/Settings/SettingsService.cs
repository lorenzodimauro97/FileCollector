
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FileCollector.Models;

namespace FileCollector.Services.Settings;

public class SettingsService
{
    private readonly string _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        try
        {
            if (!File.Exists(_appSettingsPath))
            {
                var defaultConfig = new AppConfiguration();
                await SaveConfigAsync(defaultConfig);
                return defaultConfig.AppSettings;
            }

            var json = await File.ReadAllTextAsync(_appSettingsPath);
            var config = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppConfiguration);
            return config?.AppSettings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");

            return new AppSettings();
        }
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        AppConfiguration config;
        if (File.Exists(_appSettingsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_appSettingsPath);
                config = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppConfiguration) ?? new AppConfiguration();
            }
            catch (JsonException)
            {
                Console.WriteLine("appsettings.json was malformed. Creating a new one for saving.");
                config = new AppConfiguration();
            }
        }
        else
        {
            config = new AppConfiguration();
        }
            
        config.AppSettings = settings;
        config.AppSettings.IgnorePatterns = settings.IgnorePatterns.Distinct().ToList();


        await SaveConfigAsync(config);
    }

    private async Task SaveConfigAsync(AppConfiguration config)
    {
        try
        {
            var newJson = JsonSerializer.Serialize(config, AppJsonSerializerContext.Default.AppConfiguration);
            await File.WriteAllTextAsync(_appSettingsPath, newJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public async Task<List<string>> GetIgnorePatternsAsync()
    {
        var settings = await GetAppSettingsAsync();
        return settings.IgnorePatterns;
    }

    public async Task SaveIgnorePatternsAsync(List<string> patterns)
    {
        var settings = await GetAppSettingsAsync();
        settings.IgnorePatterns = patterns;
        await SaveAppSettingsAsync(settings);
    }
}

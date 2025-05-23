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

    public async Task<List<string>> GetIgnorePatternsAsync()
    {
        try
        {
            if (!File.Exists(_appSettingsPath))
            {
                var defaultConfig = new AppConfiguration();

                await SaveAppSettingsAsync(defaultConfig);
                return defaultConfig.AppSettings.IgnorePatterns;
            }

            var json = await File.ReadAllTextAsync(_appSettingsPath);

            var config = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppConfiguration);
            return config?.AppSettings.IgnorePatterns ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
            return [];
        }
    }

    public async Task SaveIgnorePatternsAsync(List<string> patterns)
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
                Console.WriteLine("appsettings.json was malformed. Creating a new one.");
                config = new AppConfiguration();
            }
        }
        else
        {
            config = new AppConfiguration();
        }
            
        config.AppSettings.IgnorePatterns = patterns.Distinct().ToList();
        await SaveAppSettingsAsync(config);
    }

    private async Task SaveAppSettingsAsync(AppConfiguration config)
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
}
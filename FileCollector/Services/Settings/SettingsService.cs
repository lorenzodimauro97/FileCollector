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
            var settings = config?.AppSettings ?? new AppSettings();

            // Migration logic
            var settingsModified = false;
#pragma warning disable CS0618 // Type or member is obsolete
            if (!string.IsNullOrEmpty(settings.PrePrompt))
            {
                if (!settings.PrePrompts.Any())
                {
                    var newPrompt = new Prompt { Name = "Default", Content = settings.PrePrompt };
                    settings.PrePrompts.Add(newPrompt);
                    if (settings.ActivePrePromptId == null)
                    {
                        settings.ActivePrePromptId = newPrompt.Id;
                    }
                }
                settings.PrePrompt = string.Empty;
                settingsModified = true;
            }

            if (!string.IsNullOrEmpty(settings.PostPrompt))
            {
                if (!settings.PostPrompts.Any())
                {
                    var newPrompt = new Prompt { Name = "Default", Content = settings.PostPrompt };
                    settings.PostPrompts.Add(newPrompt);
                    if (settings.ActivePostPromptId == null)
                    {
                        settings.ActivePostPromptId = newPrompt.Id;
                    }
                }
                settings.PostPrompt = string.Empty;
                settingsModified = true;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (settingsModified)
            {
                await SaveAppSettingsAsync(settings);
            }

            return settings;
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
using System.Text.Json.Serialization;
using FileCollector.Models;

namespace FileCollector.Services.Settings;

[JsonSourceGenerationOptions(
    WriteIndented = true, 
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(AppConfiguration))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(SavedContext))]
[JsonSerializable(typeof(UpdateSettings))]
[JsonSerializable(typeof(Prompt))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
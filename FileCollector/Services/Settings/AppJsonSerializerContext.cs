using System.Text.Json.Serialization;
using FileCollector.Models;

namespace FileCollector.Services.Settings;

[JsonSourceGenerationOptions(
    WriteIndented = true, 
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(AppConfiguration))]

public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
using System.Text.Json;

namespace ShipmentReport.Cli;

public sealed record AppSettings(string BaseUrl, string ApiKey)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    public static AppSettings Load(string path) =>
        JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Json)
            ?? throw new JsonException($"{path} contains no settings.");
}

using System.Text.Json;
using ShipmentReport.Cli;
using ShipmentReport.Core;

var reportPath = args.Length > 0 ? args[0] : "report.json";

try
{
    var settings = AppSettings.Load("appsettings.json");

    using var http = new HttpClient { BaseAddress = new Uri(settings.BaseUrl) };
    http.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);

    var client = new ShipmentApiClient(http);
    var results = new List<ValidationResult>();

    await foreach (var dto in client.GetAllShipmentsAsync())
        results.Add(ShipmentValidator.Validate(dto));

    var summary = ReportSummary.From(results);
    await ReportWriter.WriteAsync(summary, reportPath);

    Console.WriteLine($"{results.Count} shipments, {summary.Rejected.Count} rejected. Report written to {Path.GetFullPath(reportPath)}");
    return 0;
}
catch (Exception ex) when (ex is IOException or JsonException or HttpRequestException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

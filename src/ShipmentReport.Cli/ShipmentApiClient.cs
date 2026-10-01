using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ShipmentReport.Core.Dto;

namespace ShipmentReport.Cli;

public sealed class ShipmentApiClient(HttpClient http)
{
    private const int MaxAttempts = 5;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<ShipmentPageDto> GetPageAsync(int page, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var response = await http.GetAsync($"v1/shipments?page={page}", ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ShipmentPageDto>(Json, ct)
                    ?? throw new InvalidOperationException($"Page {page} returned an empty body.");

            if (attempt == MaxAttempts || !IsTransient(response.StatusCode))
                throw new HttpRequestException(
                    $"Page {page} failed with {(int)response.StatusCode} {response.ReasonPhrase} after {attempt} attempt(s).",
                    null,
                    response.StatusCode);

            await Task.Delay(RetryDelay(response, attempt), ct);
        }
    }

    public async IAsyncEnumerable<ShipmentRecordDto> GetAllShipmentsAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var totalPages = 1;

        for (var page = 1; page <= totalPages; page++)
        {
            var dto = await GetPageAsync(page, ct);
            totalPages = dto.TotalPages;

            foreach (var shipment in dto.Shipments ?? [])
                yield return shipment;
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt) =>
        response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
}

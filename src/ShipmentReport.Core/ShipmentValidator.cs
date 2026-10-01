using System.Globalization;
using ShipmentReport.Core.Dto;

namespace ShipmentReport.Core;

public static class ShipmentValidator
{
    private static readonly string[] StatusTimeFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"
    ];

    public static ValidationResult Validate(ShipmentRecordDto dto)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.ShipmentId))
            problems.Add("shipment_id is missing");

        var status = ParseStatus(dto.Status, problems);
        var statusTime = ParseStatusTime(dto.StatusTime, problems);
        var totalWeight = ParseTotalWeight(dto.TotalWeight, problems);
        var reasonCode = CheckReason(dto.Reason, status, problems);

        if (problems.Count == 0
            && (dto.ShipmentId, status, statusTime, totalWeight) is (string id, ShipmentStatus s, DateTimeOffset time, decimal weight))
            return new ValidationResult.Valid(new ShipmentRecord(id, s, time, weight, reasonCode));

        return new ValidationResult.Rejected(dto, problems);
    }

    private static ShipmentStatus? ParseStatus(string? value, List<string> problems)
    {
        ShipmentStatus? status = value switch
        {
            "departed" => ShipmentStatus.Departed,
            "enroute" => ShipmentStatus.Enroute,
            "delayed" => ShipmentStatus.Delayed,
            "arrived" => ShipmentStatus.Arrived,
            _ => null
        };

        if (status is null)
            problems.Add($"status '{value}' is not one of departed, enroute, delayed, arrived");

        return status;
    }

    private static DateTimeOffset? ParseStatusTime(string? value, List<string> problems)
    {
        if (DateTimeOffset.TryParseExact(value, StatusTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var statusTime))
            return statusTime;

        problems.Add($"status_time '{value}' is not an ISO 8601 date-time");
        return null;
    }

    private static decimal? ParseTotalWeight(string? value, List<string> problems)
    {
        const NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        if (!decimal.TryParse(value, styles, CultureInfo.InvariantCulture, out var totalWeight))
        {
            problems.Add($"total_weight '{value}' is not a number");
            return null;
        }

        if (totalWeight < 0)
        {
            problems.Add($"total_weight '{value}' is negative");
            return null;
        }

        return totalWeight;
    }

    private static string? CheckReason(ReasonDto? reason, ShipmentStatus? status, List<string> problems)
    {
        var code = string.IsNullOrWhiteSpace(reason?.Code) ? null : reason.Code;

        if (status == ShipmentStatus.Delayed && code is null)
            problems.Add("reason is missing for a delayed shipment");

        if (status is ShipmentStatus s && s != ShipmentStatus.Delayed && code is not null)
            problems.Add($"reason '{code}' is present but status is '{s}'");

        return code;
    }
}

public abstract record ValidationResult
{
    private ValidationResult() { }

    public sealed record Valid(ShipmentRecord Shipment) : ValidationResult;

    public sealed record Rejected(ShipmentRecordDto Raw, IReadOnlyList<string> Problems) : ValidationResult;
}

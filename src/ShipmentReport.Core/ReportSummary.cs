namespace ShipmentReport.Core;

public sealed record ReportSummary(
    IReadOnlyDictionary<ShipmentStatus, int> CountByStatus,
    IReadOnlyList<DelayedShipment> Delayed,
    decimal WeightNotArrived,
    IReadOnlyList<ValidationResult.Rejected> Rejected)
{
    public static ReportSummary From(IEnumerable<ValidationResult> results)
    {
        var list = results.ToList();
        var shipments = list.OfType<ValidationResult.Valid>().Select(v => v.Shipment).ToList();

        return new ReportSummary(
            Enum.GetValues<ShipmentStatus>().ToDictionary(status => status, status => shipments.Count(s => s.Status == status)),
            shipments.Where(s => s.Status == ShipmentStatus.Delayed)
                .Select(s => new DelayedShipment(s.ShipmentId, s.ReasonCode, s.StatusTime))
                .ToList(),
            shipments.Where(s => s.Status != ShipmentStatus.Arrived).Sum(s => s.TotalWeight),
            list.OfType<ValidationResult.Rejected>().ToList());
    }
}

public sealed record DelayedShipment(string ShipmentId, string? ReasonCode, DateTimeOffset StatusTime);

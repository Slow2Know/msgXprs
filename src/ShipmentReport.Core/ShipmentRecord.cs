namespace ShipmentReport.Core;

public sealed record ShipmentRecord(
    string ShipmentId,
    ShipmentStatus Status,
    DateTimeOffset StatusTime,
    decimal TotalWeight,
    string? ReasonCode);

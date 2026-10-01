namespace ShipmentReport.Core.Dto;

public sealed record ShipmentPageDto(
    int Page,
    int TotalPages,
    IReadOnlyList<ShipmentRecordDto>? Shipments);

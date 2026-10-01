namespace ShipmentReport.Core.Dto;

public sealed record ShipmentRecordDto(
    string? ShipmentId,
    string? CarrierNumber,
    string? Status,
    string? StatusTime,
    LocationDto? Origin,
    LocationDto? Destination,
    string? TotalWeight,
    ReasonDto? Reason);

using System.ComponentModel.DataAnnotations;

namespace AspNetCoreDomainLab.Modules.Logistics;

// Bab 12 — EF Core: entity logistics dipetakan ke schema logistics.
public sealed class Shipment
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "created";
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public List<CarrierEvent> CarrierEvents { get; set; } = [];
}

public sealed class WarehouseStock
{
    public Guid Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class DeliveryRoute
{
    public Guid Id { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string Status { get; set; } = "planned";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<RouteStop> Stops { get; set; } = [];
}

public sealed class RouteStop
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public string StopCode { get; set; } = string.Empty;
    public string AddressLabel { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string Status { get; set; } = "planned";
    public DeliveryRoute? Route { get; set; }
}

public sealed class CarrierEvent
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadSummary { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public Shipment? Shipment { get; set; }
}

// Bab 8 — Validation: request records menetapkan panjang, required field, dan range input.
public sealed record CreateShipmentRequest(
    [param: Required, StringLength(40, MinimumLength = 4)] string TrackingNumber,
    [param: Required, StringLength(120, MinimumLength = 2)] string Origin,
    [param: Required, StringLength(120, MinimumLength = 2)] string Destination);

public sealed record UpdateShipmentStatusRequest(
    [param: Required, StringLength(32, MinimumLength = 2)] string Status,
    [param: Required, StringLength(120, MinimumLength = 2)] string CurrentLocation);

public sealed record CreateWarehouseStockRequest(
    [param: Required, StringLength(32, MinimumLength = 2)] string WarehouseCode,
    [param: Required, StringLength(64, MinimumLength = 2)] string Sku,
    [param: Range(0, int.MaxValue)] int Quantity);

public sealed record AdjustWarehouseStockRequest(
    [param: Range(-100000, 100000)] int QuantityDelta);

public sealed record CreateRouteRequest(
    [param: Required, StringLength(40, MinimumLength = 2)] string RouteCode);

public sealed record CreateRouteStopRequest(
    [param: Required, StringLength(40, MinimumLength = 2)] string StopCode,
    [param: Required, StringLength(160, MinimumLength = 2)] string AddressLabel,
    [param: Range(1, 1000)] int Sequence);

public sealed record CarrierEventRequest(
    [param: Required, StringLength(80, MinimumLength = 2)] string ExternalReference,
    [param: Required, StringLength(64, MinimumLength = 2)] string EventType,
    [param: Required, StringLength(240, MinimumLength = 2)] string PayloadSummary);

public sealed record ShipmentResponse(
    Guid Id,
    string TrackingNumber,
    string Status,
    string Origin,
    string Destination,
    string CurrentLocation,
    DateTimeOffset UpdatedAtUtc);

public sealed record WarehouseStockResponse(
    Guid Id,
    string WarehouseCode,
    string Sku,
    int Quantity,
    int ReservedQuantity,
    DateTimeOffset UpdatedAtUtc);

public sealed record RouteResponse(
    Guid Id,
    string RouteCode,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<RouteStopResponse> Stops);

public sealed record RouteStopResponse(
    Guid Id,
    Guid RouteId,
    string StopCode,
    string AddressLabel,
    int Sequence,
    string Status);

public sealed record CarrierEventResponse(
    Guid Id,
    Guid ShipmentId,
    string ExternalReference,
    string EventType,
    string PayloadSummary,
    DateTimeOffset ReceivedAtUtc);

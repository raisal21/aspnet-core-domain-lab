using System.ComponentModel.DataAnnotations;

namespace AspNetCoreDomainLab.Modules.IndustrialAutomation;

// Bab 12 — EF Core: entity industrial menyimpan asset, telemetry, alarm, work order, dan command.
public sealed class IndustrialAsset
{
    public Guid Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int Version { get; set; }
    public List<IndustrialTelemetryReading> Telemetry { get; set; } = [];
    public List<IndustrialAlarm> Alarms { get; set; } = [];
    public List<MaintenanceWorkOrder> WorkOrders { get; set; } = [];
}

public sealed class IndustrialTelemetryReading
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public IndustrialAsset? Asset { get; set; }
}

public sealed class IndustrialAlarm
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Severity { get; set; } = "warning";
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public DateTimeOffset RaisedAtUtc { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public IndustrialAsset? Asset { get; set; }
}

public sealed class MaintenanceWorkOrder
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public IndustrialAsset? Asset { get; set; }
}

public sealed class DeviceCommand
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string Status { get; set; } = "accepted-simulation-only";
    public DateTimeOffset IssuedAtUtc { get; set; }
    public IndustrialAsset? Asset { get; set; }
}

// Bab 8 — Validation: request records memakai DataAnnotations untuk batas input API.
public sealed record CreateIndustrialAssetRequest(
    [param: Required, StringLength(40, MinimumLength = 2)] string AssetCode,
    [param: Required, StringLength(120, MinimumLength = 2)] string DisplayName);

public sealed record UpdateAssetStatusRequest(
    [param: Required, StringLength(32, MinimumLength = 2)] string Status);

public sealed record CreateTelemetryRequest(
    decimal Value,
    [param: Required, StringLength(32, MinimumLength = 1)] string Unit,
    DateTimeOffset ObservedAtUtc);

public sealed record AcknowledgeAlarmRequest(
    [param: Required, StringLength(200, MinimumLength = 2)] string Note);

public sealed record CreateWorkOrderRequest(
    [param: Required, StringLength(160, MinimumLength = 2)] string Title,
    [param: Required, StringLength(32, MinimumLength = 2)] string Priority);

public sealed record IssueDeviceCommandRequest(
    [param: Required, StringLength(64, MinimumLength = 3)] string IdempotencyKey,
    [param: Required, StringLength(64, MinimumLength = 2)] string CommandType);

public sealed record IndustrialAssetResponse(
    Guid Id,
    string AssetCode,
    string DisplayName,
    string Status,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record TelemetryResponse(
    Guid Id,
    Guid AssetId,
    DateTimeOffset ObservedAtUtc,
    decimal Value,
    string Unit);

public sealed record AlarmResponse(
    Guid Id,
    Guid AssetId,
    string Severity,
    string Message,
    string Status,
    DateTimeOffset RaisedAtUtc,
    DateTimeOffset? AcknowledgedAtUtc);

public sealed record WorkOrderResponse(
    Guid Id,
    Guid AssetId,
    string Title,
    string Priority,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record DeviceCommandResponse(
    Guid Id,
    Guid AssetId,
    string IdempotencyKey,
    string CommandType,
    string Status,
    DateTimeOffset IssuedAtUtc);

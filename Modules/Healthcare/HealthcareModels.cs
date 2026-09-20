using System.ComponentModel.DataAnnotations;

namespace AspNetCoreDomainLab.Modules.Healthcare;

// Bab 12 — EF Core: class berikut adalah entity yang dipetakan ke schema healthcare.
public sealed class Patient
{
    public Guid Id { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<Appointment> Appointments { get; set; } = [];
    public List<LabResult> LabResults { get; set; } = [];
}

public sealed class Appointment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = "scheduled";
    public Patient? Patient { get; set; }
}

public sealed class LabResult
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTimeOffset CollectedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public Patient? Patient { get; set; }
}

public sealed class MedicalInventoryItem
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

// Bab 8 — Validation: attributes pada constructor parameter memvalidasi input HTTP sebelum service.
public sealed record CreatePatientRequest(
    [param: Required, StringLength(32, MinimumLength = 3)] string MedicalRecordNumber,
    [param: Required, StringLength(120, MinimumLength = 2)] string DisplayName,
    DateOnly BirthDate);

public sealed record CreateAppointmentRequest(
    DateTimeOffset StartsAtUtc,
    [param: Required, StringLength(120, MinimumLength = 2)] string Provider);

public sealed record CreateLabResultRequest(
    [param: Required, StringLength(120, MinimumLength = 2)] string TestName,
    [param: Required, StringLength(80, MinimumLength = 1)] string ResultValue,
    [param: Required, StringLength(32, MinimumLength = 1)] string Unit,
    DateTimeOffset CollectedAtUtc);

public sealed record ReviewLabResultRequest(
    [param: Required, StringLength(32, MinimumLength = 2)] string Decision);

public sealed record CreateInventoryItemRequest(
    [param: Required, StringLength(40, MinimumLength = 2)] string Sku,
    [param: Required, StringLength(120, MinimumLength = 2)] string Name,
    [param: Range(0, int.MaxValue)] int Quantity,
    [param: Range(0, int.MaxValue)] int ReorderLevel);

public sealed record AdjustInventoryRequest(
    [param: Range(-100000, 100000)] int QuantityDelta);

public sealed record PatientResponse(
    Guid Id,
    string MedicalRecordNumber,
    string DisplayName,
    DateOnly BirthDate,
    DateTimeOffset CreatedAtUtc);

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    DateTimeOffset StartsAtUtc,
    string Provider,
    string Status);

public sealed record LabResultResponse(
    Guid Id,
    Guid PatientId,
    string TestName,
    string ResultValue,
    string Unit,
    string Status,
    DateTimeOffset CollectedAtUtc,
    DateTimeOffset? ReviewedAtUtc);

public sealed record InventoryItemResponse(
    Guid Id,
    string Sku,
    string Name,
    int Quantity,
    int ReorderLevel,
    DateTimeOffset UpdatedAtUtc);

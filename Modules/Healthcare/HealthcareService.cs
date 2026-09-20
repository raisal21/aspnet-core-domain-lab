using AspNetCoreDomainLab.Infrastructure.Api;
using AspNetCoreDomainLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreDomainLab.Modules.Healthcare;

// Bab 6 — logika workflow REST berada di luar controller.
// Bab 9 — aturan domain menjadi typed exception yang ditangani middleware.
// Bab 10 — structured logger membuat aksi healthcare dapat didiagnosis.
// Bab 12 — query EF Core memakai context bersama dan entity set eksplisit.
public sealed class HealthcareService(
    DomainDbContext dbContext,
    ILogger<HealthcareService> logger)
{
    // Bab 12: check uniqueness in the database, persist the entity, then return a DTO.
    public async Task<PatientResponse> CreatePatientAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var medicalRecordNumber = request.MedicalRecordNumber.Trim().ToUpperInvariant();
        if (await dbContext.Patients.AnyAsync(
                patient => patient.MedicalRecordNumber == medicalRecordNumber,
                cancellationToken))
        {
            throw Rule(
                "patient_already_exists",
                "A patient with that medical record number already exists.",
                StatusCodes.Status409Conflict,
                medicalRecordNumber);
        }

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            MedicalRecordNumber = medicalRecordNumber,
            DisplayName = request.DisplayName.Trim(),
            BirthDate = request.BirthDate,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Created healthcare patient {PatientId} with medical record {MedicalRecordNumber}",
            patient.Id,
            patient.MedicalRecordNumber);
        return ToResponse(patient);
    }

    public async Task<PatientResponse> GetPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients.AsNoTracking()
            .SingleOrDefaultAsync(patient => patient.Id == patientId, cancellationToken)
            ?? throw Rule(
                "patient_not_found",
                "The requested patient was not found.",
                StatusCodes.Status404NotFound,
                patientId.ToString());
        return ToResponse(patient);
    }

    // Bab 9: future-only appointment is a domain invariant, not only a shape validation rule.
    public async Task<AppointmentResponse> CreateAppointmentAsync(
        Guid patientId,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(patientId, cancellationToken);
        if (request.StartsAtUtc <= DateTimeOffset.UtcNow)
        {
            throw Rule(
                "appointment_must_be_future",
                "An appointment must start in the future.",
                StatusCodes.Status422UnprocessableEntity,
                patientId.ToString());
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            StartsAtUtc = request.StartsAtUtc,
            Provider = request.Provider.Trim(),
            Status = "scheduled",
        };
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Scheduled healthcare appointment {AppointmentId} for patient {PatientId}",
            appointment.Id,
            patientId);
        return ToResponse(appointment);
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetAppointmentsAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(patientId, cancellationToken);
        return await dbContext.Appointments.AsNoTracking()
            .Where(appointment => appointment.PatientId == patientId)
            .OrderBy(appointment => appointment.StartsAtUtc)
            .Select(appointment => new AppointmentResponse(
                appointment.Id,
                appointment.PatientId,
                appointment.StartsAtUtc,
                appointment.Provider,
                appointment.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<LabResultResponse> CreateLabResultAsync(
        Guid patientId,
        CreateLabResultRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(patientId, cancellationToken);
        var result = new LabResult
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            TestName = request.TestName.Trim(),
            ResultValue = request.ResultValue.Trim(),
            Unit = request.Unit.Trim(),
            Status = "pending",
            CollectedAtUtc = request.CollectedAtUtc == default
                ? DateTimeOffset.UtcNow
                : request.CollectedAtUtc,
        };
        dbContext.LabResults.Add(result);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Recorded lab result {LabResultId} for patient {PatientId}",
            result.Id,
            patientId);
        return ToResponse(result);
    }

    public async Task<LabResultResponse> ReviewLabResultAsync(
        Guid labResultId,
        ReviewLabResultRequest request,
        CancellationToken cancellationToken)
    {
        var result = await dbContext.LabResults
            .SingleOrDefaultAsync(item => item.Id == labResultId, cancellationToken)
            ?? throw Rule(
                "lab_result_not_found",
                "The requested lab result was not found.",
                StatusCodes.Status404NotFound,
                labResultId.ToString());
        var decision = request.Decision.Trim().ToLowerInvariant();
        if (decision is not ("reviewed" or "flagged"))
        {
            throw Rule(
                "unsupported_lab_decision",
                "A lab result decision must be reviewed or flagged.",
                StatusCodes.Status422UnprocessableEntity,
                result.PatientId.ToString());
        }

        if (result.Status != "pending")
        {
            throw Rule(
                "lab_result_already_reviewed",
                "The lab result has already been reviewed.",
                StatusCodes.Status409Conflict,
                result.PatientId.ToString());
        }

        result.Status = decision;
        result.ReviewedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Reviewed lab result {LabResultId} with decision {Decision}",
            result.Id,
            decision);
        return ToResponse(result);
    }

    public async Task<IReadOnlyList<LabResultResponse>> GetLabResultsAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(patientId, cancellationToken);
        return await dbContext.LabResults.AsNoTracking()
            .Where(result => result.PatientId == patientId)
            .OrderByDescending(result => result.CollectedAtUtc)
            .Select(result => new LabResultResponse(
                result.Id,
                result.PatientId,
                result.TestName,
                result.ResultValue,
                result.Unit,
                result.Status,
                result.CollectedAtUtc,
                result.ReviewedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryItemResponse> CreateInventoryItemAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await dbContext.MedicalInventoryItems.AnyAsync(
                item => item.Sku == sku,
                cancellationToken))
        {
            throw Rule(
                "inventory_item_already_exists",
                "A medical inventory item with that SKU already exists.",
                StatusCodes.Status409Conflict,
                sku);
        }

        var item = new MedicalInventoryItem
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            ReorderLevel = request.ReorderLevel,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.MedicalInventoryItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created medical inventory item {Sku}", sku);
        return ToResponse(item);
    }

    public async Task<IReadOnlyList<InventoryItemResponse>> GetInventoryAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.MedicalInventoryItems.AsNoTracking()
            .OrderBy(item => item.Sku)
            .Select(item => new InventoryItemResponse(
                item.Id,
                item.Sku,
                item.Name,
                item.Quantity,
                item.ReorderLevel,
                item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    // Bab 9/12: reject an invalid stock state before saving the EF-tracked entity.
    public async Task<InventoryItemResponse> AdjustInventoryAsync(
        Guid itemId,
        AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.QuantityDelta == 0)
        {
            throw Rule(
                "inventory_adjustment_zero",
                "An inventory adjustment must change the quantity.",
                StatusCodes.Status422UnprocessableEntity,
                itemId.ToString());
        }

        var item = await dbContext.MedicalInventoryItems
            .SingleOrDefaultAsync(inventoryItem => inventoryItem.Id == itemId, cancellationToken)
            ?? throw Rule(
                "inventory_item_not_found",
                "The requested medical inventory item was not found.",
                StatusCodes.Status404NotFound,
                itemId.ToString());
        var newQuantity = item.Quantity + request.QuantityDelta;
        if (newQuantity < 0)
        {
            throw Rule(
                "inventory_quantity_negative",
                "An inventory adjustment cannot make quantity negative.",
                StatusCodes.Status422UnprocessableEntity,
                item.Sku);
        }

        item.Quantity = newQuantity;
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Adjusted medical inventory {Sku} by {QuantityDelta}",
            item.Sku,
            request.QuantityDelta);
        return ToResponse(item);
    }

    private async Task EnsurePatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Patients.AnyAsync(
                patient => patient.Id == patientId,
                cancellationToken))
        {
            throw Rule(
                "patient_not_found",
                "The requested patient was not found.",
                StatusCodes.Status404NotFound,
                patientId.ToString());
        }
    }

    private static PatientResponse ToResponse(Patient patient) => new(
        patient.Id,
        patient.MedicalRecordNumber,
        patient.DisplayName,
        patient.BirthDate,
        patient.CreatedAtUtc);

    private static AppointmentResponse ToResponse(Appointment appointment) => new(
        appointment.Id,
        appointment.PatientId,
        appointment.StartsAtUtc,
        appointment.Provider,
        appointment.Status);

    private static LabResultResponse ToResponse(LabResult result) => new(
        result.Id,
        result.PatientId,
        result.TestName,
        result.ResultValue,
        result.Unit,
        result.Status,
        result.CollectedAtUtc,
        result.ReviewedAtUtc);

    private static InventoryItemResponse ToResponse(MedicalInventoryItem item) => new(
        item.Id,
        item.Sku,
        item.Name,
        item.Quantity,
        item.ReorderLevel,
        item.UpdatedAtUtc);

    private static DomainRuleException Rule(
        string code,
        string message,
        int statusCode,
        string? reference = null) => new(
        DomainArea.Healthcare,
        code,
        message,
        statusCode,
        reference);
}

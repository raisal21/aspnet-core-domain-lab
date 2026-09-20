using Asp.Versioning;
using AspNetCoreDomainLab.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreDomainLab.Modules.Healthcare;

// Bab 2 — Controllers: HTTP boundary meneruskan pekerjaan ke HealthcareService.
// Bab 6 — REST: resource nouns, verbs, route constraints, dan Created/Ok response dipakai
// untuk membentuk workflow patient, appointment, lab result, dan inventory.
// Bab 7 — API Versioning: semua route healthcare berada di /api/v{version}.
// Bab 8 — Validation: [ApiController] memproses validation attributes pada request record.
// Bab 11 — Authorization: read/write policy membatasi operasi healthcare.
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/healthcare")]
public sealed class HealthcareController(HealthcareService service) : ControllerBase
{
    [HttpPost("patients")]
    [Authorize(Policy = AuthPolicies.HealthcareWrite)]
    public async Task<ActionResult<PatientResponse>> CreatePatient(
        CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreatePatientAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPatient), new { patientId = response.Id }, response);
    }

    [HttpGet("patients/{patientId:guid}")]
    [Authorize(Policy = AuthPolicies.HealthcareRead)]
    public async Task<ActionResult<PatientResponse>> GetPatient(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetPatientAsync(patientId, cancellationToken));
    }

    [HttpPost("patients/{patientId:guid}/appointments")]
    [Authorize(Policy = AuthPolicies.HealthcareWrite)]
    public async Task<ActionResult<AppointmentResponse>> CreateAppointment(
        Guid patientId,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAppointmentAsync(
            patientId,
            request,
            cancellationToken);
        return Created($"{Request.PathBase}/api/v1/healthcare/appointments/{response.Id}", response);
    }

    [HttpGet("patients/{patientId:guid}/appointments")]
    [Authorize(Policy = AuthPolicies.HealthcareRead)]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetAppointments(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAppointmentsAsync(patientId, cancellationToken));
    }

    [HttpPost("patients/{patientId:guid}/lab-results")]
    [Authorize(Policy = AuthPolicies.HealthcareWrite)]
    public async Task<ActionResult<LabResultResponse>> CreateLabResult(
        Guid patientId,
        CreateLabResultRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateLabResultAsync(
            patientId,
            request,
            cancellationToken);
        return Created($"{Request.PathBase}/api/v1/healthcare/lab-results/{response.Id}", response);
    }

    [HttpGet("patients/{patientId:guid}/lab-results")]
    [Authorize(Policy = AuthPolicies.HealthcareRead)]
    public async Task<ActionResult<IReadOnlyList<LabResultResponse>>> GetLabResults(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetLabResultsAsync(patientId, cancellationToken));
    }

    [HttpPost("lab-results/{labResultId:guid}/review")]
    [Authorize(Policy = AuthPolicies.HealthcareWrite)]
    public async Task<ActionResult<LabResultResponse>> ReviewLabResult(
        Guid labResultId,
        ReviewLabResultRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ReviewLabResultAsync(
            labResultId,
            request,
            cancellationToken));
    }

    [HttpPost("inventory")]
    [Authorize(Policy = AuthPolicies.HealthcareWrite)]
    public async Task<ActionResult<InventoryItemResponse>> CreateInventoryItem(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateInventoryItemAsync(request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/healthcare/inventory/{response.Id}", response);
    }

    [HttpGet("inventory")]
    [Authorize(Policy = AuthPolicies.HealthcareRead)]
    public async Task<ActionResult<IReadOnlyList<InventoryItemResponse>>> GetInventory(
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetInventoryAsync(cancellationToken));
    }

    [HttpPost("inventory/{itemId:guid}/adjust")]
    [Authorize(Policy = AuthPolicies.HealthcareWrite)]
    public async Task<ActionResult<InventoryItemResponse>> AdjustInventory(
        Guid itemId,
        AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.AdjustInventoryAsync(itemId, request, cancellationToken));
    }
}

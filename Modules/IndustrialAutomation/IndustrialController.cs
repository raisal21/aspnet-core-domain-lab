using Asp.Versioning;
using AspNetCoreDomainLab.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace AspNetCoreDomainLab.Modules.IndustrialAutomation;

// Bab 2/6 — Controller REST boundary untuk asset, telemetry, alarm, maintenance, dan command.
// Bab 7 — URL-segment versioning menjaga contract industrial tetap eksplisit.
// Bab 11 — Policy memisahkan operator dari maintainer.
// Bab 13/15 — attribute metadata menempelkan output cache dan rate limit command;
// Bab 14 — compression ditentukan oleh provider host berdasarkan path domain.
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/industrial")]
public sealed class IndustrialController(IndustrialService service) : ControllerBase
{
    [HttpPost("assets")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    public async Task<ActionResult<IndustrialAssetResponse>> CreateAsset(
        CreateIndustrialAssetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAssetAsync(request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/industrial/assets/{response.Id}", response);
    }

    [HttpGet("assets")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    public async Task<ActionResult<IReadOnlyList<IndustrialAssetResponse>>> GetAssets(
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAssetsAsync(cancellationToken));
    }

    [HttpGet("assets/{assetId:guid}")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    public async Task<ActionResult<IndustrialAssetResponse>> GetAsset(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAssetAsync(assetId, cancellationToken));
    }

    [HttpPost("assets/{assetId:guid}/status")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    public async Task<ActionResult<IndustrialAssetResponse>> UpdateStatus(
        Guid assetId,
        UpdateAssetStatusRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateStatusAsync(assetId, request, cancellationToken));
    }

    [HttpPost("assets/{assetId:guid}/telemetry")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    public async Task<ActionResult<TelemetryResponse>> RecordTelemetry(
        Guid assetId,
        CreateTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        return Accepted(await service.RecordTelemetryAsync(assetId, request, cancellationToken));
    }

    [HttpGet("assets/{assetId:guid}/telemetry")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    [OutputCache(PolicyName = "industrial-telemetry")]
    public async Task<ActionResult<IReadOnlyList<TelemetryResponse>>> GetTelemetry(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetTelemetryAsync(assetId, cancellationToken));
    }

    [HttpGet("assets/{assetId:guid}/alarms")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    public async Task<ActionResult<IReadOnlyList<AlarmResponse>>> GetAlarms(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAlarmsAsync(assetId, cancellationToken));
    }

    [HttpPost("alarms/{alarmId:guid}/acknowledge")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    public async Task<ActionResult<AlarmResponse>> AcknowledgeAlarm(
        Guid alarmId,
        AcknowledgeAlarmRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.AcknowledgeAlarmAsync(alarmId, request, cancellationToken));
    }

    [HttpPost("assets/{assetId:guid}/maintenance")]
    [Authorize(Policy = AuthPolicies.IndustrialMaintain)]
    public async Task<ActionResult<WorkOrderResponse>> CreateWorkOrder(
        Guid assetId,
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        return Created($"{Request.PathBase}/api/v1/industrial/maintenance", await service.CreateWorkOrderAsync(
            assetId,
            request,
            cancellationToken));
    }

    [HttpGet("assets/{assetId:guid}/maintenance")]
    [Authorize(Policy = AuthPolicies.IndustrialMaintain)]
    public async Task<ActionResult<IReadOnlyList<WorkOrderResponse>>> GetWorkOrders(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetWorkOrdersAsync(assetId, cancellationToken));
    }

    [HttpPost("maintenance/{workOrderId:guid}/complete")]
    [Authorize(Policy = AuthPolicies.IndustrialMaintain)]
    public async Task<ActionResult<WorkOrderResponse>> CompleteWorkOrder(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.CompleteWorkOrderAsync(workOrderId, cancellationToken));
    }

    [HttpPost("assets/{assetId:guid}/commands")]
    [Authorize(Policy = AuthPolicies.IndustrialOperate)]
    [EnableRateLimiting("industrial-command")]
    public async Task<ActionResult<DeviceCommandResponse>> IssueCommand(
        Guid assetId,
        IssueDeviceCommandRequest request,
        CancellationToken cancellationToken)
    {
        return Accepted(await service.IssueCommandAsync(assetId, request, cancellationToken));
    }
}

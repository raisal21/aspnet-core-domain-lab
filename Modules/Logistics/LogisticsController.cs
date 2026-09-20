using Asp.Versioning;
using AspNetCoreDomainLab.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace AspNetCoreDomainLab.Modules.Logistics;

// Bab 2/6 — Controller REST boundary untuk shipment, stock, route, stop, dan carrier event.
// Bab 7 — URL-segment versioning menjaga contract logistics dapat berevolusi.
// Bab 11 — read dan dispatch memakai authorization policy yang berbeda.
// Bab 13/15 — read mendapat output cache; mutation dispatch mendapat fixed-window limiter.
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/logistics")]
public sealed class LogisticsController(LogisticsService service) : ControllerBase
{
    [HttpPost("shipments")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<ShipmentResponse>> CreateShipment(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateShipmentAsync(request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/logistics/shipments/{response.TrackingNumber}", response);
    }

    [HttpGet("shipments/{trackingNumber}")]
    [Authorize(Policy = AuthPolicies.LogisticsRead)]
    [OutputCache(PolicyName = "logistics-read")]
    public async Task<ActionResult<ShipmentResponse>> GetShipment(
        string trackingNumber,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetShipmentAsync(trackingNumber, cancellationToken));
    }

    [HttpPost("shipments/{trackingNumber}/status")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<ShipmentResponse>> UpdateShipmentStatus(
        string trackingNumber,
        UpdateShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateShipmentStatusAsync(
            trackingNumber,
            request,
            cancellationToken));
    }

    [HttpPost("warehouse/stock")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<WarehouseStockResponse>> CreateWarehouseStock(
        CreateWarehouseStockRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateWarehouseStockAsync(request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/logistics/warehouse/stock/{response.Id}", response);
    }

    [HttpGet("warehouse/{warehouseCode}/stock")]
    [Authorize(Policy = AuthPolicies.LogisticsRead)]
    [OutputCache(PolicyName = "logistics-read")]
    public async Task<ActionResult<IReadOnlyList<WarehouseStockResponse>>> GetWarehouseStock(
        string warehouseCode,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetWarehouseStockAsync(warehouseCode, cancellationToken));
    }

    [HttpPost("warehouse/stock/{stockId:guid}/adjust")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<WarehouseStockResponse>> AdjustWarehouseStock(
        Guid stockId,
        AdjustWarehouseStockRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.AdjustWarehouseStockAsync(stockId, request, cancellationToken));
    }

    [HttpPost("routes")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<RouteResponse>> CreateRoute(
        CreateRouteRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateRouteAsync(request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/logistics/routes/{response.Id}", response);
    }

    [HttpPost("routes/{routeId:guid}/stops")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<RouteResponse>> AddStop(
        Guid routeId,
        CreateRouteStopRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.AddStopAsync(routeId, request, cancellationToken));
    }

    [HttpPost("stops/{stopId:guid}/complete")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<RouteStopResponse>> CompleteStop(
        Guid stopId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.CompleteStopAsync(stopId, cancellationToken));
    }

    [HttpPost("shipments/{trackingNumber}/carrier-events")]
    [Authorize(Policy = AuthPolicies.LogisticsDispatch)]
    [EnableRateLimiting("logistics-dispatch")]
    public async Task<ActionResult<CarrierEventResponse>> ReceiveCarrierEvent(
        string trackingNumber,
        CarrierEventRequest request,
        CancellationToken cancellationToken)
    {
        return Accepted(await service.ReceiveCarrierEventAsync(
            trackingNumber,
            request,
            cancellationToken));
    }
}

using AspNetCoreDomainLab.Infrastructure.Api;
using AspNetCoreDomainLab.Infrastructure.Caching;
using AspNetCoreDomainLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreDomainLab.Modules.Logistics;

// Bab 6 — service menjaga resource workflow di balik route REST.
// Bab 9 — shipment/stock/route invariant menghasilkan error domain yang terarah.
// Bab 10 — structured logging membantu diagnosis carrier dan dispatch.
// Bab 12 — EF Core menyimpan aggregate dan integration-boundary event.
// Bab 13 — shipment/warehouse read memakai cache dengan invalidation pada mutation.
public sealed class LogisticsService(
    DomainDbContext dbContext,
    IDomainMemoryCache cache,
    ILogger<LogisticsService> logger)
{
    public async Task<ShipmentResponse> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var trackingNumber = request.TrackingNumber.Trim().ToUpperInvariant();
        if (await dbContext.Shipments.AnyAsync(
                shipment => shipment.TrackingNumber == trackingNumber,
                cancellationToken))
        {
            throw Rule(
                "shipment_already_exists",
                "A shipment with that tracking number already exists.",
                StatusCodes.Status409Conflict,
                trackingNumber);
        }

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Status = "created",
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            CurrentLocation = request.Origin.Trim(),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.Shipments.Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created shipment {TrackingNumber}", trackingNumber);
        return ToResponse(shipment);
    }

    // Bab 13: cache shipment dibaca sebelum database dan memiliki TTL 15 detik.
    public async Task<ShipmentResponse> GetShipmentAsync(
        string trackingNumber,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeTracking(trackingNumber);
        var key = ShipmentCacheKey(normalized);
        if (cache.TryGet<ShipmentResponse>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var shipment = await dbContext.Shipments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TrackingNumber == normalized, cancellationToken)
            ?? throw Rule(
                "shipment_not_found",
                "The requested shipment was not found.",
                StatusCodes.Status404NotFound,
                normalized);
        var response = ToResponse(shipment);
        cache.Set(key, response, TimeSpan.FromSeconds(15));
        return response;
    }

    public async Task<ShipmentResponse> UpdateShipmentStatusAsync(
        string trackingNumber,
        UpdateShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeTracking(trackingNumber);
        var status = request.Status.Trim().ToLowerInvariant();
        if (status is not ("created" or "in-transit" or "delivered" or "exception"))
        {
            throw Rule(
                "unsupported_shipment_status",
                "Shipment status must be created, in-transit, delivered, or exception.",
                StatusCodes.Status422UnprocessableEntity,
                normalized);
        }

        var shipment = await dbContext.Shipments
            .SingleOrDefaultAsync(item => item.TrackingNumber == normalized, cancellationToken)
            ?? throw Rule(
                "shipment_not_found",
                "The requested shipment was not found.",
                StatusCodes.Status404NotFound,
                normalized);
        shipment.Status = status;
        shipment.CurrentLocation = request.CurrentLocation.Trim();
        shipment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(ShipmentCacheKey(normalized));
        logger.LogInformation("Updated shipment {TrackingNumber} to {Status}", normalized, status);
        return ToResponse(shipment);
    }

    public async Task<WarehouseStockResponse> CreateWarehouseStockAsync(
        CreateWarehouseStockRequest request,
        CancellationToken cancellationToken)
    {
        var warehouseCode = request.WarehouseCode.Trim().ToUpperInvariant();
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await dbContext.WarehouseStocks.AnyAsync(
                item => item.WarehouseCode == warehouseCode && item.Sku == sku,
                cancellationToken))
        {
            throw Rule(
                "warehouse_stock_already_exists",
                "That SKU already exists in the warehouse.",
                StatusCodes.Status409Conflict,
                warehouseCode);
        }

        var stock = new WarehouseStock
        {
            Id = Guid.NewGuid(),
            WarehouseCode = warehouseCode,
            Sku = sku,
            Quantity = request.Quantity,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.WarehouseStocks.Add(stock);
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(WarehouseCacheKey(warehouseCode));
        logger.LogInformation("Created warehouse stock {WarehouseCode}/{Sku}", warehouseCode, sku);
        return ToResponse(stock);
    }

    public async Task<IReadOnlyList<WarehouseStockResponse>> GetWarehouseStockAsync(
        string warehouseCode,
        CancellationToken cancellationToken)
    {
        var normalized = warehouseCode.Trim().ToUpperInvariant();
        var key = WarehouseCacheKey(normalized);
        if (cache.TryGet<IReadOnlyList<WarehouseStockResponse>>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var stock = await dbContext.WarehouseStocks.AsNoTracking()
            .Where(item => item.WarehouseCode == normalized)
            .OrderBy(item => item.Sku)
            .Select(item => new WarehouseStockResponse(
                item.Id,
                item.WarehouseCode,
                item.Sku,
                item.Quantity,
                item.ReservedQuantity,
                item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        cache.Set(key, stock, TimeSpan.FromSeconds(5));
        return stock;
    }

    public async Task<WarehouseStockResponse> AdjustWarehouseStockAsync(
        Guid stockId,
        AdjustWarehouseStockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.QuantityDelta == 0)
        {
            throw Rule(
                "warehouse_adjustment_zero",
                "A warehouse adjustment must change the quantity.",
                StatusCodes.Status422UnprocessableEntity);
        }

        var stock = await dbContext.WarehouseStocks
            .SingleOrDefaultAsync(item => item.Id == stockId, cancellationToken)
            ?? throw Rule(
                "warehouse_stock_not_found",
                "The requested warehouse stock row was not found.",
                StatusCodes.Status404NotFound);
        var newQuantity = stock.Quantity + request.QuantityDelta;
        if (newQuantity < stock.ReservedQuantity)
        {
            throw Rule(
                "warehouse_quantity_below_reserved",
                "Warehouse quantity cannot fall below the reserved quantity.",
                StatusCodes.Status422UnprocessableEntity,
                stock.WarehouseCode);
        }

        stock.Quantity = newQuantity;
        stock.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(WarehouseCacheKey(stock.WarehouseCode));
        logger.LogInformation("Adjusted warehouse stock {WarehouseCode}/{Sku}", stock.WarehouseCode, stock.Sku);
        return ToResponse(stock);
    }

    public async Task<RouteResponse> CreateRouteAsync(
        CreateRouteRequest request,
        CancellationToken cancellationToken)
    {
        var routeCode = request.RouteCode.Trim().ToUpperInvariant();
        if (await dbContext.DeliveryRoutes.AnyAsync(
                route => route.RouteCode == routeCode,
                cancellationToken))
        {
            throw Rule(
                "route_already_exists",
                "A delivery route with that code already exists.",
                StatusCodes.Status409Conflict,
                routeCode);
        }

        var route = new DeliveryRoute
        {
            Id = Guid.NewGuid(),
            RouteCode = routeCode,
            Status = "planned",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.DeliveryRoutes.Add(route);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created delivery route {RouteCode}", routeCode);
        return ToResponse(route);
    }

    // Bab 12: stop ditambahkan langsung ke DbSet agar EF tidak salah menganggapnya sebagai update.
    public async Task<RouteResponse> AddStopAsync(
        Guid routeId,
        CreateRouteStopRequest request,
        CancellationToken cancellationToken)
    {
        var route = await dbContext.DeliveryRoutes
            .Include(item => item.Stops)
            .SingleOrDefaultAsync(item => item.Id == routeId, cancellationToken)
            ?? throw Rule(
                "route_not_found",
                "The requested delivery route was not found.",
                StatusCodes.Status404NotFound,
                routeId.ToString());
        if (route.Stops.Any(stop => stop.Sequence == request.Sequence))
        {
            throw Rule(
                "route_sequence_exists",
                "A route stop already uses that sequence number.",
                StatusCodes.Status409Conflict,
                route.RouteCode);
        }

        var stop = new RouteStop
        {
            Id = Guid.NewGuid(),
            RouteId = routeId,
            StopCode = request.StopCode.Trim().ToUpperInvariant(),
            AddressLabel = request.AddressLabel.Trim(),
            Sequence = request.Sequence,
            Status = "planned",
        };
        dbContext.RouteStops.Add(stop);
        await dbContext.SaveChangesAsync(cancellationToken);
        var stops = await dbContext.RouteStops.AsNoTracking()
            .Where(item => item.RouteId == routeId)
            .OrderBy(item => item.Sequence)
            .Select(item => new RouteStopResponse(
                item.Id,
                item.RouteId,
                item.StopCode,
                item.AddressLabel,
                item.Sequence,
                item.Status))
            .ToListAsync(cancellationToken);
        logger.LogInformation("Added stop {StopCode} to route {RouteCode}", request.StopCode, route.RouteCode);
        return new RouteResponse(route.Id, route.RouteCode, route.Status, route.CreatedAtUtc, stops);
    }

    public async Task<RouteStopResponse> CompleteStopAsync(
        Guid stopId,
        CancellationToken cancellationToken)
    {
        var stop = await dbContext.RouteStops
            .SingleOrDefaultAsync(item => item.Id == stopId, cancellationToken)
            ?? throw Rule(
                "route_stop_not_found",
                "The requested route stop was not found.",
                StatusCodes.Status404NotFound);
        if (stop.Status == "completed")
        {
            throw Rule(
                "route_stop_already_completed",
                "The route stop has already been completed.",
                StatusCodes.Status409Conflict,
                stop.StopCode);
        }

        stop.Status = "completed";
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Completed route stop {StopCode}", stop.StopCode);
        return ToResponse(stop);
    }

    // Bab 6/9: carrier event adalah integration boundary idempotent; event duplikat mengembalikan hasil lama.
    public async Task<CarrierEventResponse> ReceiveCarrierEventAsync(
        string trackingNumber,
        CarrierEventRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeTracking(trackingNumber);
        var shipment = await dbContext.Shipments
            .SingleOrDefaultAsync(item => item.TrackingNumber == normalized, cancellationToken)
            ?? throw Rule(
                "shipment_not_found",
                "The carrier event references an unknown shipment.",
                StatusCodes.Status404NotFound,
                normalized,
                retryable: true);
        var externalReference = request.ExternalReference.Trim();
        var existing = await dbContext.CarrierEvents.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ExternalReference == externalReference,
                cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        var carrierEvent = new CarrierEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            ExternalReference = externalReference,
            EventType = request.EventType.Trim().ToLowerInvariant(),
            PayloadSummary = request.PayloadSummary.Trim(),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.CarrierEvents.Add(carrierEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(ShipmentCacheKey(normalized));
        logger.LogInformation(
            "Accepted carrier event {ExternalReference} for shipment {TrackingNumber}",
            externalReference,
            normalized);
        return ToResponse(carrierEvent);
    }

    private static string NormalizeTracking(string trackingNumber) => trackingNumber.Trim().ToUpperInvariant();

    private static string ShipmentCacheKey(string trackingNumber) => $"logistics:shipment:{trackingNumber}";

    private static string WarehouseCacheKey(string warehouseCode) => $"logistics:warehouse:{warehouseCode}";

    private static ShipmentResponse ToResponse(Shipment shipment) => new(
        shipment.Id,
        shipment.TrackingNumber,
        shipment.Status,
        shipment.Origin,
        shipment.Destination,
        shipment.CurrentLocation,
        shipment.UpdatedAtUtc);

    private static WarehouseStockResponse ToResponse(WarehouseStock stock) => new(
        stock.Id,
        stock.WarehouseCode,
        stock.Sku,
        stock.Quantity,
        stock.ReservedQuantity,
        stock.UpdatedAtUtc);

    private static RouteResponse ToResponse(DeliveryRoute route) => new(
        route.Id,
        route.RouteCode,
        route.Status,
        route.CreatedAtUtc,
        route.Stops
            .OrderBy(stop => stop.Sequence)
            .Select(ToResponse)
            .ToArray());

    private static RouteStopResponse ToResponse(RouteStop stop) => new(
        stop.Id,
        stop.RouteId,
        stop.StopCode,
        stop.AddressLabel,
        stop.Sequence,
        stop.Status);

    private static CarrierEventResponse ToResponse(CarrierEvent carrierEvent) => new(
        carrierEvent.Id,
        carrierEvent.ShipmentId,
        carrierEvent.ExternalReference,
        carrierEvent.EventType,
        carrierEvent.PayloadSummary,
        carrierEvent.ReceivedAtUtc);

    private static DomainRuleException Rule(
        string code,
        string message,
        int statusCode,
        string? reference = null,
        bool retryable = false) => new(
        DomainArea.Logistics,
        code,
        message,
        statusCode,
        reference,
        retryable);
}

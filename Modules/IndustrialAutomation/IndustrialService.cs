using AspNetCoreDomainLab.Infrastructure.Api;
using AspNetCoreDomainLab.Infrastructure.Caching;
using AspNetCoreDomainLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreDomainLab.Modules.IndustrialAutomation;

// Bab 6 — service mengorkestrasi workflow asset/telemetry/maintenance.
// Bab 9 — state transition dan synthetic threshold menghasilkan domain error.
// Bab 10 — event penting dan alarm dicatat dengan structured logging.
// Bab 12 — EF Core menyimpan entity dan concurrency version.
// Bab 13 — telemetry memakai memory cache dengan invalidation saat data baru masuk.
public sealed class IndustrialService(
    DomainDbContext dbContext,
    IDomainMemoryCache cache,
    ILogger<IndustrialService> logger)
{
    public async Task<IndustrialAssetResponse> CreateAssetAsync(
        CreateIndustrialAssetRequest request,
        CancellationToken cancellationToken)
    {
        var assetCode = request.AssetCode.Trim().ToUpperInvariant();
        if (await dbContext.IndustrialAssets.AnyAsync(
                asset => asset.AssetCode == assetCode,
                cancellationToken))
        {
            throw Rule(
                "asset_already_exists",
                "An industrial asset with that code already exists.",
                StatusCodes.Status409Conflict,
                assetCode);
        }

        var now = DateTimeOffset.UtcNow;
        var asset = new IndustrialAsset
        {
            Id = Guid.NewGuid(),
            AssetCode = assetCode,
            DisplayName = request.DisplayName.Trim(),
            Status = "offline",
            LastSeenAtUtc = now,
            CreatedAtUtc = now,
        };
        dbContext.IndustrialAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created industrial asset {AssetCode}", assetCode);
        return ToResponse(asset);
    }

    public async Task<IReadOnlyList<IndustrialAssetResponse>> GetAssetsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.IndustrialAssets.AsNoTracking()
            .OrderBy(asset => asset.AssetCode)
            .Select(asset => new IndustrialAssetResponse(
                asset.Id,
                asset.AssetCode,
                asset.DisplayName,
                asset.Status,
                asset.LastSeenAtUtc,
                asset.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IndustrialAssetResponse> GetAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var asset = await GetAssetEntityAsync(assetId, cancellationToken);
        return ToResponse(asset);
    }

    public async Task<IndustrialAssetResponse> UpdateStatusAsync(
        Guid assetId,
        UpdateAssetStatusRequest request,
        CancellationToken cancellationToken)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        if (status is not ("online" or "offline" or "maintenance" or "fault"))
        {
            throw Rule(
                "unsupported_asset_status",
                "Asset status must be online, offline, maintenance, or fault.",
                StatusCodes.Status422UnprocessableEntity,
                assetId.ToString());
        }

        var asset = await GetAssetEntityAsync(assetId, cancellationToken);
        asset.Status = status;
        asset.LastSeenAtUtc = DateTimeOffset.UtcNow;
        asset.Version++;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Set industrial asset {AssetCode} status to {Status}", asset.AssetCode, status);
        return ToResponse(asset);
    }

    // Bab 13: mutation menghapus cache sebelum pembacaan berikutnya mengambil data terbaru.
    public async Task<TelemetryResponse> RecordTelemetryAsync(
        Guid assetId,
        CreateTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await GetAssetEntityAsync(assetId, cancellationToken);
        var now = request.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : request.ObservedAtUtc;
        var telemetry = new IndustrialTelemetryReading
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            ObservedAtUtc = now,
            Value = request.Value,
            Unit = request.Unit.Trim(),
        };
        asset.Status = "online";
        asset.LastSeenAtUtc = now;
        asset.Version++;
        dbContext.IndustrialTelemetryReadings.Add(telemetry);
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(TelemetryCacheKey(assetId));

        if (request.Value >= 1000)
        {
            dbContext.IndustrialAlarms.Add(new IndustrialAlarm
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Severity = "critical",
                Message = $"Synthetic threshold exceeded: {request.Value} {request.Unit}",
                Status = "open",
                RaisedAtUtc = now,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Raised a critical synthetic alarm for asset {AssetCode} from telemetry value {Value}",
                asset.AssetCode,
                request.Value);
        }

        logger.LogInformation("Recorded telemetry for industrial asset {AssetCode}", asset.AssetCode);
        return ToResponse(telemetry);
    }

    // Bab 13: cache hit menghindari query berulang; miss mengambil maksimal 100 reading lalu memberi TTL.
    public async Task<IReadOnlyList<TelemetryResponse>> GetTelemetryAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        await GetAssetEntityAsync(assetId, cancellationToken);
        var key = TelemetryCacheKey(assetId);
        if (cache.TryGet<IReadOnlyList<TelemetryResponse>>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var telemetry = await dbContext.IndustrialTelemetryReadings.AsNoTracking()
            .Where(reading => reading.AssetId == assetId)
            .OrderByDescending(reading => reading.ObservedAtUtc)
            .Take(100)
            .Select(reading => new TelemetryResponse(
                reading.Id,
                reading.AssetId,
                reading.ObservedAtUtc,
                reading.Value,
                reading.Unit))
            .ToListAsync(cancellationToken);
        cache.Set(key, telemetry, TimeSpan.FromSeconds(3));
        return telemetry;
    }

    public async Task<IReadOnlyList<AlarmResponse>> GetAlarmsAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        await GetAssetEntityAsync(assetId, cancellationToken);
        return await dbContext.IndustrialAlarms.AsNoTracking()
            .Where(alarm => alarm.AssetId == assetId)
            .OrderByDescending(alarm => alarm.RaisedAtUtc)
            .Select(alarm => new AlarmResponse(
                alarm.Id,
                alarm.AssetId,
                alarm.Severity,
                alarm.Message,
                alarm.Status,
                alarm.RaisedAtUtc,
                alarm.AcknowledgedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AlarmResponse> AcknowledgeAlarmAsync(
        Guid alarmId,
        AcknowledgeAlarmRequest request,
        CancellationToken cancellationToken)
    {
        var alarm = await dbContext.IndustrialAlarms
            .SingleOrDefaultAsync(item => item.Id == alarmId, cancellationToken)
            ?? throw Rule(
                "alarm_not_found",
                "The requested industrial alarm was not found.",
                StatusCodes.Status404NotFound,
                alarmId.ToString());
        if (alarm.Status == "acknowledged")
        {
            throw Rule(
                "alarm_already_acknowledged",
                "The industrial alarm has already been acknowledged.",
                StatusCodes.Status409Conflict,
                alarm.AssetId.ToString());
        }

        alarm.Status = "acknowledged";
        alarm.AcknowledgedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Acknowledged industrial alarm {AlarmId}: {Note}", alarmId, request.Note.Trim());
        return new AlarmResponse(
            alarm.Id,
            alarm.AssetId,
            alarm.Severity,
            alarm.Message,
            alarm.Status,
            alarm.RaisedAtUtc,
            alarm.AcknowledgedAtUtc);
    }

    public async Task<WorkOrderResponse> CreateWorkOrderAsync(
        Guid assetId,
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await GetAssetEntityAsync(assetId, cancellationToken);
        var priority = request.Priority.Trim().ToLowerInvariant();
        if (priority is not ("low" or "normal" or "high" or "critical"))
        {
            throw Rule(
                "unsupported_work_order_priority",
                "Work order priority must be low, normal, high, or critical.",
                StatusCodes.Status422UnprocessableEntity,
                asset.AssetCode);
        }

        var workOrder = new MaintenanceWorkOrder
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Title = request.Title.Trim(),
            Priority = priority,
            Status = "open",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.MaintenanceWorkOrders.Add(workOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created maintenance work order {WorkOrderId} for {AssetCode}", workOrder.Id, asset.AssetCode);
        return ToResponse(workOrder);
    }

    public async Task<IReadOnlyList<WorkOrderResponse>> GetWorkOrdersAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        await GetAssetEntityAsync(assetId, cancellationToken);
        return await dbContext.MaintenanceWorkOrders.AsNoTracking()
            .Where(order => order.AssetId == assetId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => new WorkOrderResponse(
                order.Id,
                order.AssetId,
                order.Title,
                order.Priority,
                order.Status,
                order.CreatedAtUtc,
                order.CompletedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderResponse> CompleteWorkOrderAsync(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.MaintenanceWorkOrders
            .SingleOrDefaultAsync(order => order.Id == workOrderId, cancellationToken)
            ?? throw Rule(
                "work_order_not_found",
                "The requested maintenance work order was not found.",
                StatusCodes.Status404NotFound,
                workOrderId.ToString());
        if (workOrder.Status == "completed")
        {
            throw Rule(
                "work_order_already_completed",
                "The maintenance work order has already been completed.",
                StatusCodes.Status409Conflict,
                workOrder.AssetId.ToString());
        }

        workOrder.Status = "completed";
        workOrder.CompletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Completed maintenance work order {WorkOrderId}", workOrderId);
        return ToResponse(workOrder);
    }

    // Bab 6/9: idempotency key membuat retry command aman; status menegaskan simulasi tidak mengendalikan hardware.
    public async Task<DeviceCommandResponse> IssueCommandAsync(
        Guid assetId,
        IssueDeviceCommandRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await GetAssetEntityAsync(assetId, cancellationToken);
        var key = request.IdempotencyKey.Trim();
        var existing = await dbContext.DeviceCommands.AsNoTracking()
            .SingleOrDefaultAsync(command => command.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            if (existing.AssetId != assetId)
            {
                throw Rule(
                    "command_idempotency_conflict",
                    "The idempotency key belongs to another asset.",
                    StatusCodes.Status409Conflict,
                    asset.AssetCode);
            }

            return ToResponse(existing);
        }

        var command = new DeviceCommand
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            IdempotencyKey = key,
            CommandType = request.CommandType.Trim().ToLowerInvariant(),
            Status = "accepted-simulation-only",
            IssuedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.DeviceCommands.Add(command);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning(
            "Accepted simulation-only device command {CommandType} for asset {AssetCode}",
            command.CommandType,
            asset.AssetCode);
        return ToResponse(command);
    }

    private async Task<IndustrialAsset> GetAssetEntityAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        return await dbContext.IndustrialAssets
            .SingleOrDefaultAsync(asset => asset.Id == assetId, cancellationToken)
            ?? throw Rule(
                "asset_not_found",
                "The requested industrial asset was not found.",
                StatusCodes.Status404NotFound,
                assetId.ToString());
    }

    private static string TelemetryCacheKey(Guid assetId) => $"industrial:telemetry:{assetId}";

    private static IndustrialAssetResponse ToResponse(IndustrialAsset asset) => new(
        asset.Id,
        asset.AssetCode,
        asset.DisplayName,
        asset.Status,
        asset.LastSeenAtUtc,
        asset.CreatedAtUtc);

    private static TelemetryResponse ToResponse(IndustrialTelemetryReading reading) => new(
        reading.Id,
        reading.AssetId,
        reading.ObservedAtUtc,
        reading.Value,
        reading.Unit);

    private static WorkOrderResponse ToResponse(MaintenanceWorkOrder order) => new(
        order.Id,
        order.AssetId,
        order.Title,
        order.Priority,
        order.Status,
        order.CreatedAtUtc,
        order.CompletedAtUtc);

    private static DeviceCommandResponse ToResponse(DeviceCommand command) => new(
        command.Id,
        command.AssetId,
        command.IdempotencyKey,
        command.CommandType,
        command.Status,
        command.IssuedAtUtc);

    private static DomainRuleException Rule(
        string code,
        string message,
        int statusCode,
        string? reference = null,
        bool retryable = false) => new(
        DomainArea.Industrial,
        code,
        message,
        statusCode,
        reference,
        retryable);
}

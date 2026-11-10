using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;
using RepairDesk.Api.Models.Enums;
using RepairDesk.Api.Services;

namespace RepairDesk.Api.BackgroundServices;

/// <summary>
/// Periodically retries Backordered cases so a Replace resolution that had no
/// stock at the time gets fulfilled automatically once a restock arrives,
/// instead of waiting for an agent to notice and resolve it by hand.
/// </summary>
public class BackorderFulfillmentService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackorderFulfillmentService> _logger;

    public BackorderFulfillmentService(IServiceScopeFactory scopeFactory, ILogger<BackorderFulfillmentService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FulfillPendingBackordersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error while trying to fulfill backordered RMA cases.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task FulfillPendingBackordersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RepairDeskDbContext>();
        var caseService = scope.ServiceProvider.GetRequiredService<IRmaCaseService>();

        var backorderedIds = await db.RmaCases
            .Where(c => c.Status == CaseStatus.Backordered)
            .Select(c => c.Id)
            .ToListAsync(stoppingToken);

        foreach (var caseId in backorderedIds)
        {
            var fulfilled = await caseService.TryFulfillBackorderAsync(caseId, stoppingToken);
            if (fulfilled)
            {
                _logger.LogInformation("Backordered case {CaseId} fulfilled from restocked inventory.", caseId);
            }
        }
    }
}

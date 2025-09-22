using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using JobTracker.Infrastructure.Jobs;          // InMemoryJobQueue, BulkBillingCommand
using JobTracker.Application.Services;        // IBillingBatchProcessor
using JobTracker.Infrastructure.Data;         // AppDb
using JobTracker.Infrastructure.Data.Entities; // JobExecution
using JobTracker.Api.Workers;

namespace JobTracker.Api.Workers;

public sealed class JobWorker : BackgroundService
{
    private readonly InMemoryJobQueue _queue;
    private readonly IServiceProvider _sp;
    private readonly ILogger<JobWorker> _logger;

    public JobWorker(InMemoryJobQueue queue, IServiceProvider sp, ILogger<JobWorker> logger)
    {
        _queue = queue;
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobWorker started");

        await foreach (var cmd in _queue.ReadAllAsync(stoppingToken))
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            try
            {
                if (cmd is BulkBillingCommand bill)
                {
                    // Optional idempotency handling (if key provided)
                    JobExecution? exec = null;
                    if (!string.IsNullOrWhiteSpace(bill.IdempotencyKey))
                    {
                        exec = await db.JobExecutions
                            .SingleOrDefaultAsync(j => j.Key == bill.IdempotencyKey, stoppingToken);

                        if (exec is not null)
                        {
                            _logger.LogInformation(
                                "BulkBilling skipped: duplicate idempotency key {Key} (status {Status})",
                                bill.IdempotencyKey, exec.Status);
                            continue; // already recorded; skip duplicate work
                        }

                        exec = new JobExecution { Key = bill.IdempotencyKey!, Status = "Pending" };
                        db.JobExecutions.Add(exec);
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    var processor = scope.ServiceProvider.GetRequiredService<IBillingBatchProcessor>();

                    try
                    {
                        // Does a single set-based UPDATE inside a DB transaction (all-or-nothing)
                        await processor.ProcessAsync(bill.InvoiceIds, stoppingToken);

                        // Demo switch to force rollback
                        if (bill.ForceFail)
                            throw new Exception("Forced failure for rollback demo");

                        if (exec is not null)
                        {
                            exec.Status = "Succeeded";
                            exec.CompletedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync(stoppingToken);
                        }

                        _logger.LogInformation("Bulk billing succeeded for {Count} invoice(s).",
                            bill.InvoiceIds.Count);
                    }
                    catch (Exception ex)
                    {
                        if (exec is not null)
                        {
                            exec.Status = "Failed";
                            exec.CompletedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync(stoppingToken);
                        }

                        _logger.LogError(ex, "Bulk billing failed; transaction rolled back.");
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown command type: {Type}", cmd.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing job.");
            }
        }
    }
}

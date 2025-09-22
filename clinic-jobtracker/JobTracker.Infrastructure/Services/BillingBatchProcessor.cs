using JobTracker.Application.Services;
using JobTracker.Infrastructure.Data;   // AppDb, SqliteBulk
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace JobTracker.Infrastructure.Services;

public sealed class BillingBatchProcessor : IBillingBatchProcessor
{
    private readonly AppDb _db;
    public BillingBatchProcessor(AppDb db) => _db = db;

    public async Task ProcessAsync(IReadOnlyList<Guid> invoiceIds, CancellationToken ct = default)
    {
        if (invoiceIds.Count == 0) return;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // Bulk set-based update (atomic in SQL)
            await SqliteBulk.BulkUpdateInvoicesAsync(
                _db,
                invoiceIds.Select(id => (id, "Paid")),
                ct);

            // Simulate a failure *before commit*
            throw new Exception("force rollback demo");

            // If no exception, commit would run here
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);   // explicit rollback
            throw;                        // rethrow so caller knows it failed
        }
    }
}

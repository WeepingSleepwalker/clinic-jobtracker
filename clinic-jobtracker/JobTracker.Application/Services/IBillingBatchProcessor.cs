namespace JobTracker.Application.Services;

public interface IBillingBatchProcessor
{
    /// <summary>
    /// Processes a batch of invoice updates atomically.
    /// For demo: mark provided invoices as Paid (extend as needed).
    /// </summary>
    Task ProcessAsync(IReadOnlyList<Guid> invoiceIds, CancellationToken ct = default);
}

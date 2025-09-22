namespace JobTracker.Infrastructure.Jobs;

public sealed class BulkBillingCommand : IJobCommand
{
    public Guid JobId { get; init; } = Guid.NewGuid();
    public string? IdempotencyKey { get; set; }
    public required List<Guid> InvoiceIds { get; init; }
    public bool ForceFail { get; init; }   // <-- add this
    public string Type => "BulkBilling";
}

// Example queue API



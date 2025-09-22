namespace JobTracker.Infrastructure.Data.Entities;

public class JobExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = default!;  // idempotency key (unique)
    public string Type { get; set; } = "BulkBilling";
    public string Status { get; set; } = "Pending"; // Pending | Succeeded | Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

namespace JobTracker.Infrastructure.Jobs;

public sealed class JobStatus
{
    public Guid JobId { get; init; }
    public string State { get; set; } = "Queued";  // e.g., Queued, Processing, Completed, Failed
    public int Processed { get; set; }
    public int Total { get; set; }
    public string? Error { get; set; }
}

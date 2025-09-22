using System.Collections.Concurrent;
using System.Threading.Channels;

namespace JobTracker.Infrastructure.Jobs;

/// <summary>Lightweight in-process queue for demo purposes.</summary>
public sealed class InMemoryJobQueue
{
    private readonly Channel<IJobCommand> _channel = Channel.CreateUnbounded<IJobCommand>();

    // Thread-safe stores
    private readonly ConcurrentDictionary<Guid, JobStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, BulkBillingCommand> _idempotency = new();

    public async ValueTask EnqueueAsync(IJobCommand cmd, CancellationToken ct = default)
    {
        // Track initial status
        var total = (cmd as BulkBillingCommand)?.InvoiceIds.Count ?? 0;
        _statuses[cmd.JobId] = new JobStatus { JobId = cmd.JobId, Total = total, State = "Queued" };

        await _channel.Writer.WriteAsync(cmd, ct);
    }

    public IAsyncEnumerable<IJobCommand> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);

    // ---------- Methods your Program.cs expects ----------

    public JobStatus? GetStatus(Guid jobId)
        => _statuses.TryGetValue(jobId, out var s) ? s : null;

    public bool TryGetExistingJob(string idempotencyKey, out BulkBillingCommand cmd)
        => _idempotency.TryGetValue(idempotencyKey, out cmd!);

    // Helpers for workers / idempotency usage
    public bool RecordIdempotent(BulkBillingCommand cmd)
        => !string.IsNullOrEmpty(cmd.IdempotencyKey)
           && _idempotency.TryAdd(cmd.IdempotencyKey!, cmd);

    public void SetProcessing(Guid jobId)
    {
        if (_statuses.TryGetValue(jobId, out var s)) s.State = "Processing";
    }

    public void UpdateProgress(Guid jobId, int processed)
    {
        if (_statuses.TryGetValue(jobId, out var s)) s.Processed = processed;
    }

    public void SetCompleted(Guid jobId)
    {
        if (_statuses.TryGetValue(jobId, out var s)) s.State = "Completed";
    }

    public void SetFailed(Guid jobId, string error)
    {
        if (_statuses.TryGetValue(jobId, out var s))
        {
            s.State = "Failed";
            s.Error = error;
        }
    }
}

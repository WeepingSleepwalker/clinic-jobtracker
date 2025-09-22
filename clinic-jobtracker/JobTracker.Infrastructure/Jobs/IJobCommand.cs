namespace JobTracker.Infrastructure.Jobs;

public interface IJobCommand
{
    Guid JobId { get; }
}

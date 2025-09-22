namespace JobTracker.Domain.Entities;

public enum ApptStatus { Scheduled, Completed, Canceled }

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid DoctorId  { get; set; }
    public DateTime ScheduledAt { get; set; }  // UTC
    public int DurationMin { get; set; }
    public ApptStatus Status { get; set; } = ApptStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

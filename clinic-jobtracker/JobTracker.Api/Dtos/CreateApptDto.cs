namespace JobTracker.Api.Dtos;

public record CreateApptDto(Guid PatientId, Guid DoctorId, DateTime ScheduledAt, int DurationMin);

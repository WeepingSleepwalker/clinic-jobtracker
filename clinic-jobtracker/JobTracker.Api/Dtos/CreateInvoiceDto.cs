namespace JobTracker.Api.Dtos;
public record CreateInvoiceDto(Guid AppointmentId, int AmountCents);

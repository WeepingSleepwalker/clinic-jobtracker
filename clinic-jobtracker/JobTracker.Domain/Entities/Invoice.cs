namespace JobTracker.Domain.Entities;

public enum InvoiceStatus { Pending, Paid, Failed }

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }   // one invoice per appointment
    public int AmountCents { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}

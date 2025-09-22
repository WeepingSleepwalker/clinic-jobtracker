namespace JobTracker.Api.Dtos;
public record CompleteAndInvoiceDto(int AmountCents, bool SimulateBillingSuccess = true);

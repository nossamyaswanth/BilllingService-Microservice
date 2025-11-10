namespace BillingService.Models;

public class MarkPaidRequest
{
    public long PaymentId { get; set; }
    public decimal Amount { get; set; }
}
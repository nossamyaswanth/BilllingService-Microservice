namespace BillingService.Models;

public class Bill
{
    public long BillId { get; set; }
    public long PatientId { get; set; }
    public long AppointmentId { get; set; }
    public decimal AmountSubtotal { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal AmountTotal { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 0;
    public string? Notes { get; set; }

    public List<BillLineItem> LineItems { get; set; } = new();
}
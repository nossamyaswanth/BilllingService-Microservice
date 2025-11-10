namespace BillingService.Models;

public class CreateBillRequest
{
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public List<CreateBillLineItem> LineItems { get; set; } = new();
    public decimal? TaxPercent { get; set; }
}

public class CreateBillLineItem
{
    public string Type { get; set; } = "CONSULTATION";
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
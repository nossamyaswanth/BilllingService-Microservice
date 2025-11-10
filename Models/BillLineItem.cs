namespace BillingService.Models;
public class BillLineItem
{
    public long LineId { get; set; }
    public long BillId { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // ✅ Add setter (public or private is fine)
    public decimal LineTotal { get; set; }

    public Bill Bill { get; set; } = null!;
}
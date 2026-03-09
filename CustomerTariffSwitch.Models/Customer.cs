namespace CustomerTariffSwitch.Models;

public class Customer
{
    public required string CustomerId { get; set; }
    public required string Name { get; set; }
    public bool HasUnpaidInvoice { get; set; }
    public required string Sla { get; set; }
    public required string MeterType { get; set; }
}

namespace CustomerTariffSwitch.Models;

public class Customer
{
    public required string CustomerId { get; set; }
    public required string Name { get; set; }
    public bool HasUnpaidInvoice { get; set; }
    public SLALevel Sla { get; set; }
    public MeterType MeterType { get; set; }

    public override string ToString() =>
        $"{CustomerId} | {Name} | SLA={Sla} | Meter={MeterType} | UnpaidInvoice={HasUnpaidInvoice}";
}

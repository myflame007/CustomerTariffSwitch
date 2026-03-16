namespace CustomerTariffSwitch.Models;

public class Customer
{
    // init kann nur beim Erstellen gesetzt werden
    public required string CustomerId { get; init; }
    public required string Name { get; init; }
    public bool HasUnpaidInvoice { get; init; }
    public SLALevel Sla { get; init; }
    public MeterType MeterType { get; init; }

    public override string ToString() =>
        $"{CustomerId} | {Name} | SLA={Sla} | Meter={MeterType} | UnpaidInvoice={HasUnpaidInvoice}";
}

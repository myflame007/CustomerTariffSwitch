namespace CustomerTariffSwitch.Models;

public class Tariff
{
    public required string TariffId { get; init; }
    public required string Name { get; init; }
    public bool RequiresSmartMeter { get; init; }
    public decimal BaseMonthlyGross { get; init; }

    public override string ToString() =>
        $"{TariffId} | {Name} | SmartMeter={RequiresSmartMeter} | Price={BaseMonthlyGross}";
}

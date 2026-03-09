namespace CustomerTariffSwitch.Models;

public class Tariff
{
    public required string TariffId { get; set; }
    public required string Name { get; set; }
    public bool RequiresSmartMeter { get; set; }
    public decimal BaseMonthlyGross { get; set; }
}

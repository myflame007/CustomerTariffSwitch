namespace CustomerTariffSwitch.Models;

public class SwitchRequest
{
    public required string RequestId { get; set; }
    public required string CustomerId { get; set; }
    public required string TargetTariffId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }

    public override string ToString() =>
        $"{RequestId} | Customer={CustomerId} | Tariff={TargetTariffId} | RequestedAt={RequestedAt:O}";
}

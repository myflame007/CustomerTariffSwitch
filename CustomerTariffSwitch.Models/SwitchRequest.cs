namespace CustomerTariffSwitch.Models;

public class SwitchRequest
{
    public required string RequestId { get; init; }
    public required string CustomerId { get; init; }
    public required string TargetTariffId { get; init; }
    public DateTimeOffset RequestedAt { get; init; }

    public override string ToString() =>
        $"{RequestId} | Customer={CustomerId} | Tariff={TargetTariffId} | RequestedAt={RequestedAt:O}";
}

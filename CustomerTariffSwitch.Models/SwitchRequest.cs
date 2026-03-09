namespace CustomerTariffSwitch.Models;

public class SwitchRequest
{
    public required string RequestId { get; set; }
    public required string CustomerId { get; set; }
    public required string TargetTariffId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
}

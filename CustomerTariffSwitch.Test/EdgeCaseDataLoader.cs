using System.Text.Json;
using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Test;

internal static class EdgeCaseDataLoader
{
    public static EdgeCaseDataSet Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "edge-cases.json");
        var json = File.ReadAllText(path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<EdgeCaseDataSet>(json, options);
        if (data is null)
        {
            throw new InvalidOperationException("Failed to load edge-cases.json");
        }

        return data;
    }
}

internal sealed class EdgeCaseDataSet
{
    public required UnknownCustomerCase UnknownCustomerCase { get; init; }
}

internal sealed class UnknownCustomerCase
{
    public required SwitchRequest Request { get; init; }
    public required Tariff Tariff { get; init; }
}

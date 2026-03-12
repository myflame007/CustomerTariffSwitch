using CustomerTariffSwitch.Models;
using CustomerTariffSwitch.Services;

namespace CustomerTariffSwitch.Test;

public class SlaHoursTests
{
    private readonly List<Customer> _customers;
    private readonly List<Tariff> _tariffs;

    public SlaHoursTests()
    {
        var csvService = new CsvService();
        var (customers, _, tariffs) = csvService.ReadKnownFiles();
        _customers = customers;
        _tariffs = tariffs;
    }

    [Fact]
    public void CalculateSlaHours_Returns24_ForPremiumWithoutUpgrade()
    {
        var sut = new ProcessRequestService();
        var customer = _customers.Single(c => c.CustomerId == "C001");
        var tariff = _tariffs.Single(t => t.TariffId == "T-BASIC");

        var result = sut.CalculateSlaHours(customer, tariff);

        Assert.Equal(24, result);
    }

    [Fact]
    public void CalculateSlaHours_Returns48_ForStandardWithoutUpgrade()
    {
        var sut = new ProcessRequestService();
        var customer = _customers.Single(c => c.CustomerId == "C005");
        var tariff = _tariffs.Single(t => t.TariffId == "T-BASIC");

        var result = sut.CalculateSlaHours(customer, tariff);

        Assert.Equal(48, result);
    }

    [Fact]
    public void CalculateSlaHours_Returns60_ForStandardWithSmartMeterUpgrade()
    {
        var sut = new ProcessRequestService();
        var customer = _customers.Single(c => c.CustomerId == "C003");
        var tariff = _tariffs.Single(t => t.TariffId == "T-ECO");

        var result = sut.CalculateSlaHours(customer, tariff);

        Assert.Equal(60, result);
    }
}

using CustomerTariffSwitch.Models;
using CustomerTariffSwitch.Services;

namespace CustomerTariffSwitch.Test;

public class SlaHoursTests
{
    private readonly ProcessRequestService _sut = new();

    

    [Fact]
    public void CalculateSlaHours_Returns48_ForStandardWithoutUpgrade()
    {
        // Scenario 1: Standard SLA, no upgrade needed -> 48h
        var customer = new Customer
        {
            CustomerId = "C-1", Name = "Test",
            HasUnpaidInvoice = false,
            Sla = SLALevel.Standard,
            MeterType = MeterType.Smart
        };
        var tariff = new Tariff
        {
            TariffId = "T-1", Name = "Test",
            RequiresSmartMeter = false,
            BaseMonthlyGross = 10m
        };

        var result = _sut.CalculateSlaHours(customer, tariff);

        Assert.Equal(48, result);
    }


    [Fact]
    public void CalculateSlaHours_Returns24_ForPremiumWithoutUpgrade()
    {
        // Scenario 2: Premium SLA, smart meter already installed -> 24h
        var customer = new Customer
        {
            CustomerId = "C-1",
            Name = "Test",
            HasUnpaidInvoice = false,
            Sla = SLALevel.Premium,
            MeterType = MeterType.Smart
        };
        var tariff = new Tariff
        {
            TariffId = "T-1",
            Name = "Test",
            RequiresSmartMeter = false,
            BaseMonthlyGross = 10m
        };

        var result = _sut.CalculateSlaHours(customer, tariff);

        Assert.Equal(24, result);
    }

    [Fact]
    public void CalculateSlaHours_Returns60_ForStandardWithSmartMeterUpgrade()
    {
        // Scenario 3: Standard SLA + classic meter + smart tariff -> 48 + 12 = 60h
        var customer = new Customer
        {
            CustomerId = "C-1", Name = "Test",
            HasUnpaidInvoice = false,
            Sla = SLALevel.Standard,
            MeterType = MeterType.Classic   // <-- needs upgrade
        };
        var tariff = new Tariff
        {
            TariffId = "T-1", Name = "Test",
            RequiresSmartMeter = true,      // <-- requires smart meter
            BaseMonthlyGross = 10m
        };

        var result = _sut.CalculateSlaHours(customer, tariff);

        Assert.Equal(60, result);
    }

    [Fact]
    public void CalculateSlaHours_Returns36_ForPremiumWithSmartMeterUpgrade()
    {
        // Scenario 2+3: Premium SLA + classic meter + smart tariff -> 24 + 12 = 36h
        var customer = new Customer
        {
            CustomerId = "C-1", Name = "Test",
            HasUnpaidInvoice = false,
            Sla = SLALevel.Premium,
            MeterType = MeterType.Classic   // <-- needs upgrade
        };
        var tariff = new Tariff
        {
            TariffId = "T-1", Name = "Test",
            RequiresSmartMeter = true,      // <-- requires smart meter
            BaseMonthlyGross = 10m
        };

        var result = _sut.CalculateSlaHours(customer, tariff);

        Assert.Equal(36, result);
    }
}

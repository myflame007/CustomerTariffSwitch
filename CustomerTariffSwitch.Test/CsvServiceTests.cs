using CustomerTariffSwitch.Services;
using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Test;

public class CsvServiceTests
{
    private readonly List<Customer> _customers;
    private readonly List<SwitchRequest> _requests;
    private readonly List<Tariff> _tariffs;

    public CsvServiceTests()
    {
        var sut = new CsvService();
        var (customers, requests, tariffs) = sut.ReadKnownFiles();
        _customers = customers;
        _requests = requests;
        _tariffs = tariffs;
    }

    [Fact]
    public void ReadKnownFiles_ReturnsAllExpectedFiles()
    {
        Assert.Equal(5, _customers.Count);
        Assert.Equal(6, _requests.Count);
        Assert.Equal(3, _tariffs.Count);
    }

    [Fact]
    public void CustomersContainOneUnpaidInvoice()
    {
        var unpaidCount = _customers.Count(c => c.HasUnpaidInvoice);

        Assert.Equal(1, unpaidCount);
    }

    [Fact]
    public void CustomersContainFourPaidInvoices()
    {
        var paidCount = _customers.Count(c => !c.HasUnpaidInvoice);

        Assert.Equal(4, paidCount);
    }

}

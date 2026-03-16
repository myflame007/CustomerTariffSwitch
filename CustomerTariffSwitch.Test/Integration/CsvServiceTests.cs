using CustomerTariffSwitch.Services;
using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Test;

[Trait("Category", "Integration")]
public class CsvServiceTests
{
    private readonly List<Customer> _customers;
    private readonly List<SwitchRequest> _requests;
    private readonly List<Tariff> _tariffs;
    private readonly List<(string RawId, string Reason)> _invalidRequests;

    public CsvServiceTests()
    {
        var csvService = new CsvService();
        var (customers, requests, tariffs, invalidRequests) = csvService.ReadKnownFiles();
        _customers = customers;
        _requests = requests;
        _tariffs = tariffs;
        _invalidRequests = invalidRequests;
    }

    [Fact]
    public void ReadKnownFiles_ReturnsAllExpectedFiles()
    {
        Assert.Equal(7, _customers.Count); // includes 2 rows with incomplete data (H-3)
        Assert.Equal(6, _requests.Count);  // R1007-R1009 are invalid and excluded
        Assert.Equal(3, _tariffs.Count);
    }

    [Fact]
    public void ParseRequests_InvalidRows_AreNotInValidList()
    {
        Assert.DoesNotContain(_requests, r => r.RequestId == "R1007");
        Assert.DoesNotContain(_requests, r => r.RequestId == "R1008");
        Assert.DoesNotContain(_requests, r => r.RequestId == "R1009");
    }

    [Fact]
    public void ParseRequests_InvalidRows_AreReturnedSeparately()
    {
        Assert.Equal(3, _invalidRequests.Count);
        Assert.Contains(_invalidRequests, r => r.RawId == "R1007"); // empty field
        Assert.Contains(_invalidRequests, r => r.RawId == "R1008"); // bad timestamp
        Assert.Contains(_invalidRequests, r => r.RawId == "R1009"); // missing timestamp
    }

    [Fact]
    public void ParseRequests_InvalidRows_HaveCorrectReason()
    {
        Assert.All(_invalidRequests, r => Assert.Equal("Invalid request data", r.Reason));
    }

    [Fact]
    public void CustomersContainOneUnpaidInvoice()
    {
        var unpaidCount = _customers.Count(c => c.HasUnpaidInvoice);

        Assert.Equal(1, unpaidCount);
    }

    [Fact]
    public void CustomersContainSixPaidInvoices()
    {
        var paidCount = _customers.Count(c => !c.HasUnpaidInvoice);

        Assert.Equal(6, paidCount);
    }

    [Fact]
    public void CustomerNames_AreReadWithCorrectSpecialCharacters()
    {
        Assert.Contains(_customers, c => c.Name == "Stadtcafé GmbH");
        Assert.Contains(_customers, c => c.Name == "Miriam Hölzl");
        Assert.Contains(_customers, c => c.Name == "Bäckerei Schönbrunn KG");
    }

}

using CustomerTariffSwitch.Models;
using CustomerTariffSwitch.Services;

namespace CustomerTariffSwitch.Test;

public class ProcessRequestServiceTests
{
    private readonly List<Customer> _customers;
    private readonly List<SwitchRequest> _requests;
    private readonly List<Tariff> _tariffs;
    private readonly EdgeCaseDataSet _edgeCases = EdgeCaseDataLoader.Load();

    public ProcessRequestServiceTests()
    {
        var csvService = new CsvService();
        var (customers, requests, tariffs) = csvService.ReadKnownFiles();
        _customers = customers;
        _requests = requests;
        _tariffs = tariffs;
    }

    [Fact]
    public void ProcessRequests_RejectsRequest_WhenCustomerHasUnpaidInvoice()
    {
        var sut = new ProcessRequestService();

        var customer = _customers.Single(c => c.CustomerId == "C002");
        var request = _requests.Single(r => r.RequestId == "R1002");
        var tariff = _tariffs.Single(t => t.TariffId == "T-BASIC");

        var result = sut.ProcessRequests([customer], [request], [tariff]);

        var decision = Assert.Single(result);
        Assert.Equal("R1002", decision.RequestId);
        Assert.Equal("Rejected", decision.Status);
        Assert.Equal("Unpaid invoice", decision.Reason);
    }

    [Fact]
    public void ProcessRequests_ApprovesRequest_WhenCustomerAndTariffAreValidAndNoUnpaidInvoice()
    {
        var sut = new ProcessRequestService();

        var customer = _customers.Single(c => c.CustomerId == "C001");
        var request = _requests.Single(r => r.RequestId == "R1001");
        var tariff = _tariffs.Single(t => t.TariffId == "T-ECO");

        var result = sut.ProcessRequests([customer], [request], [tariff]);

        var decision = Assert.Single(result);
        Assert.Equal("Approved", decision.Status);
        Assert.Null(decision.Reason);
        Assert.Equal(DateTimeOffset.Parse("2025-03-31T01:15:00+02:00"), decision.DueAt);
        Assert.Null(decision.FollowUpAction);
    }

    [Fact]
    public void ProcessRequests_AddsTwelveHoursAndFollowUp_WhenSmartMeterUpgradeIsNeeded()
    {
        var sut = new ProcessRequestService();

        var customer = _customers.Single(c => c.CustomerId == "C003");
        var request = _requests.Single(r => r.RequestId == "R1003");
        var tariff = _tariffs.Single(t => t.TariffId == "T-ECO");

        var result = sut.ProcessRequests([customer], [request], [tariff]);

        var decision = Assert.Single(result);
        Assert.Equal("Approved", decision.Status);
        Assert.Equal(DateTimeOffset.Parse("2025-10-28T14:30:00+01:00"), decision.DueAt);
        Assert.Equal("Schedule meter upgrade", decision.FollowUpAction);
    }

    [Fact]
    public void ProcessRequests_RejectsRequest_WhenCustomerIsUnknown_FromEdgeCaseData()
    {
        var sut = new ProcessRequestService();
        var edge = _edgeCases.UnknownCustomerCase;

        var result = sut.ProcessRequests(_customers, [edge.Request], [edge.Tariff]);

        var decision = Assert.Single(result);
        Assert.Equal("Rejected", decision.Status);
        Assert.Equal("Unknown customer", decision.Reason);
    }
}

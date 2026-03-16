using CustomerTariffSwitch.Models;
using CustomerTariffSwitch.Services;

namespace CustomerTariffSwitch.Test;

public class ProcessRequestServiceTests
{
    private readonly ProcessRequestService _ProcessRequestService = new();

    // Summer time (CEST = UTC+2): 10:00 UTC = 12:00 Vienna local
    private static readonly DateTimeOffset SummerTimestamp = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProcessRequests_RejectsRequest_WhenCustomerHasUnpaidInvoice()
    {
        // Scenario 4: customer has unpaid invoice -> reject
        var customer = new Customer
        {
            CustomerId = "C-1",
            Name = "Test",
            HasUnpaidInvoice = true,        
            Sla = SLALevel.Standard,
            MeterType = MeterType.Smart
        };
        var tariff = new Tariff
        {
            TariffId = "T-1",
            Name = "Test",
            RequiresSmartMeter = false,
            BaseMonthlyGross = 10m
        };
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-1",
            TargetTariffId = "T-1",
            RequestedAt = SummerTimestamp
        };

        var result = _ProcessRequestService.ProcessRequests([customer], [request], [tariff], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Equal("Unpaid invoice", decision.Reason);
    }

    [Fact]
    public void ProcessRequests_RejectsRequest_WhenCustomerIsUnknown()
    {
        // Scenario 5: CustomerId in request does not exist in customer list
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-DOES-NOT-EXIST",
            TargetTariffId = "T-1",
            RequestedAt = SummerTimestamp
        };
        var tariff = new Tariff
        {
            TariffId = "T-1", Name = "Test",
            RequiresSmartMeter = false,
            BaseMonthlyGross = 10m
        };

        var result = _ProcessRequestService.ProcessRequests([], [request], [tariff], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Equal("Unknown customer", decision.Reason);
    }

    [Fact]
    public void ProcessRequests_RejectsRequest_WhenTariffIsUnknown()
    {
        // Scenario 6: TariffId in request does not exist in tariff list
        var customer = new Customer
        {
            CustomerId = "C-1", Name = "Test",
            HasUnpaidInvoice = false,
            Sla = SLALevel.Standard,
            MeterType = MeterType.Smart
        };
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-1",
            TargetTariffId = "T-DOES-NOT-EXIST",
            RequestedAt = SummerTimestamp
        };

        var result = _ProcessRequestService.ProcessRequests([customer], [request], [], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Equal("Unknown tariff", decision.Reason);
    }

   

    [Fact]
    public void ProcessRequests_ApprovesRequest_WithStandardSla_AndSets48hDueDate()
    {
        // Scenario 1: Standard SLA, no upgrade -> DueAt = RequestedAt + 48h (Vienna)
        // 10:00 UTC = 12:00 Vienna (CEST +2) -> +48h = 2025-06-03T12:00:00+02:00
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
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-1",
            TargetTariffId = "T-1",
            RequestedAt = SummerTimestamp
        };

        var result = _ProcessRequestService.ProcessRequests([customer], [request], [tariff], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Approved, decision.Status);
        Assert.Equal(DateTimeOffset.Parse("2025-06-03T12:00:00+02:00"), decision.DueAt);
        Assert.Null(decision.FollowUpAction);
    }

    [Fact]
    public void ProcessRequests_ApprovesRequest_WithPremiumSla_AndSets24hDueDate()
    {
        // Scenario 2: Premium SLA, no upgrade -> DueAt = RequestedAt + 24h (Vienna)
        // 10:00 UTC = 12:00 Vienna (CEST +2) -> +24h = 2025-06-02T12:00:00+02:00
        var customer = new Customer
        {
            CustomerId = "C-1", Name = "Test",
            HasUnpaidInvoice = false,
            Sla = SLALevel.Premium,         // <-- Premium
            MeterType = MeterType.Smart
        };
        var tariff = new Tariff
        {
            TariffId = "T-1", Name = "Test",
            RequiresSmartMeter = false,
            BaseMonthlyGross = 10m
        };
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-1",
            TargetTariffId = "T-1",
            RequestedAt = SummerTimestamp
        };

        var result = _ProcessRequestService.ProcessRequests([customer], [request], [tariff], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Approved, decision.Status);
        Assert.Equal(DateTimeOffset.Parse("2025-06-02T12:00:00+02:00"), decision.DueAt);
        Assert.Null(decision.FollowUpAction);
    }

    // DST: spring forward 2025-03-30 02:00 CET -> 03:00 CEST (gap: 02:00-02:59 is invalid)
    // Request at 2025-03-28T01:00:00Z = 2025-03-28T02:00:00 CET
    // +48h local = 2025-03-30T02:00:00 -> invalid -> pushed forward to 03:00:00 CEST (+02:00)
    [Fact]
    public void ProcessRequests_DueAt_IsAdjustedForward_WhenItFallsInSpringForwardGap()
    {
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
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-1",
            TargetTariffId = "T-1",
            RequestedAt = new DateTimeOffset(2025, 3, 28, 1, 0, 0, TimeSpan.Zero) // 02:00 CET
        };

        var result = _ProcessRequestService.ProcessRequests([customer], [request], [tariff], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Approved, decision.Status);
        // 02:00 is invalid -> pushed to 03:00 CEST (+02:00)
        Assert.Equal(DateTimeOffset.Parse("2025-03-30T03:00:00+02:00"), decision.DueAt);
    }

    // DST: fall back 2025-10-26 03:00 CEST -> 02:00 CET (02:00-02:59 occurs twice = ambiguous)
    // Request at 2025-10-24T00:00:00Z = 2025-10-24T02:00:00 CEST
    // +48h local = 2025-10-26T02:00:00 -> ambiguous -> code picks max offset = CEST (+02:00)
    [Fact]
    public void ProcessRequests_DueAt_UsesEarlierOffset_WhenItFallsInFallBackAmbiguousHour()
    {
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
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-1",
            TargetTariffId = "T-1",
            RequestedAt = new DateTimeOffset(2025, 10, 24, 0, 0, 0, TimeSpan.Zero) // 02:00 CEST
        };

        var result = _ProcessRequestService.ProcessRequests([customer], [request], [tariff], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Approved, decision.Status);
        // ambiguous 02:00 -> picks max offset = +02:00 (CEST, the earlier occurrence)
        Assert.Equal(DateTimeOffset.Parse("2025-10-26T02:00:00+02:00"), decision.DueAt);
    }

    [Fact]
    public void ProcessRequests_ApprovesWithFollowUpAndExtendedDueDate_WhenSmartMeterUpgradeNeeded()
    {
        // Scenario 3: Standard + classic meter + smart tariff -> 48 + 12 = 60h, follow-up set
        // 10:00 UTC = 12:00 Vienna (CEST +2) -> +60h = 2025-06-04T00:00:00+02:00
        var customer = new Customer
        {
            CustomerId = "C-1", Name = "Test",
            HasUnpaidInvoice = false,
            Sla = SLALevel.Standard,
            MeterType = MeterType.Classic   // <-- classic meter -> upgrade needed
        };
        var tariff = new Tariff
        {
            TariffId = "T-1", Name = "Test",
            RequiresSmartMeter = true,      // <-- requires smart meter
            BaseMonthlyGross = 10m
        };
        var request = new SwitchRequest
        {
            RequestId = "R-1",
            CustomerId = "C-1",
            TargetTariffId = "T-1",
            RequestedAt = SummerTimestamp
        };

        var result = _ProcessRequestService.ProcessRequests([customer], [request], [tariff], []);

        var decision = Assert.Single(result);
        Assert.Equal(DecisionStatus.Approved, decision.Status);
        Assert.Equal(DateTimeOffset.Parse("2025-06-04T00:00:00+02:00"), decision.DueAt);
        Assert.Equal("Schedule meter upgrade", decision.FollowUpAction);
    }
}

using CustomerTariffSwitch.Models;
using CustomerTariffSwitch.Services;

namespace CustomerTariffSwitch.Test;

// Unit tests for CsvService parsing methods - no filesystem access, pure in-memory rows
// The rows format mirrors what CsvService produces after splitting lines by ';'
public class CsvParsingTests
{
    // ------- ParseCustomers -------

    [Fact]
    public void ParseCustomers_ReturnsEmptyList_ForHeaderOnly()
    {
        var rows = Rows("CustomerId;Name;HasUnpaidInvoice;SLA;MeterType");

        var result = CsvService.ParseCustomers(rows);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseCustomers_ParsesSingleValidRow()
    {
        var rows = Rows(
            "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType",
            "C001;Anna Maier;FALSE;Premium;Smart");

        var result = CsvService.ParseCustomers(rows);

        var customer = Assert.Single(result);
        Assert.Equal("C001", customer.CustomerId);
        Assert.Equal("Anna Maier", customer.Name);
        Assert.False(customer.HasUnpaidInvoice);
        Assert.Equal(SLALevel.Premium, customer.Sla);
        Assert.Equal(MeterType.Smart, customer.MeterType);
    }

    [Fact]
    public void ParseCustomers_ParsesTrueUnpaidInvoice()
    {
        var rows = Rows(
            "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType",
            "C002;Test GmbH;TRUE;Standard;Classic");

        var result = CsvService.ParseCustomers(rows);

        Assert.True(result[0].HasUnpaidInvoice);
        Assert.Equal(MeterType.Classic, result[0].MeterType);
    }

    [Fact]
    public void ParseCustomers_SkipsRows_WithTooFewColumns()
    {
        var rows = Rows(
            "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType",
            "C001;Anna Maier;FALSE;Premium"); // only 4 columns

        var result = CsvService.ParseCustomers(rows);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseCustomers_ParsesMultipleRows()
    {
        var rows = Rows(
            "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType",
            "C001;Anna Maier;FALSE;Premium;Smart",
            "C002;Test GmbH;TRUE;Standard;Classic",
            "C003;Jamal Idris;FALSE;Standard;Classic");

        var result = CsvService.ParseCustomers(rows);

        Assert.Equal(3, result.Count);
    }

    // ------- ParseRequests -------

    [Fact]
    public void ParseRequests_ReturnsEmptyLists_ForHeaderOnly()
    {
        var rows = Rows("RequestId;CustomerId;TargetTariffId;RequestedAtISO8601");

        var (valid, invalid) = CsvService.ParseRequests(rows);

        Assert.Empty(valid);
        Assert.Empty(invalid);
    }

    [Fact]
    public void ParseRequests_ParsesSingleValidRow()
    {
        var rows = Rows(
            "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601",
            "R1001;C001;T-ECO;2025-06-15T11:20:00+02:00");

        var (valid, invalid) = CsvService.ParseRequests(rows);

        var request = Assert.Single(valid);
        Assert.Equal("R1001", request.RequestId);
        Assert.Equal("C001", request.CustomerId);
        Assert.Equal("T-ECO", request.TargetTariffId);
        Assert.Equal(DateTimeOffset.Parse("2025-06-15T11:20:00+02:00"), request.RequestedAt);
        Assert.Empty(invalid);
    }

    [Fact]
    public void ParseRequests_InvalidRow_EmptyRequiredField_GoesToInvalidList()
    {
        var rows = Rows(
            "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601",
            "R1007;C001;;2025-06-01T10:00:00+02:00"); // empty TargetTariffId

        var (valid, invalid) = CsvService.ParseRequests(rows);

        Assert.Empty(valid);
        var entry = Assert.Single(invalid);
        Assert.Equal("R1007", entry.RawId);
        Assert.Equal("Invalid request data", entry.Reason);
    }

    [Fact]
    public void ParseRequests_InvalidRow_MalformedTimestamp_GoesToInvalidList()
    {
        var rows = Rows(
            "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601",
            "R1008;C002;T-BASIC;not-a-date");

        var (valid, invalid) = CsvService.ParseRequests(rows);

        Assert.Empty(valid);
        var entry = Assert.Single(invalid);
        Assert.Equal("R1008", entry.RawId);
    }

    [Fact]
    public void ParseRequests_InvalidRow_TooFewColumns_GoesToInvalidList()
    {
        var rows = Rows(
            "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601",
            "R1009;C001;T-BASIC"); // missing timestamp column

        var (valid, invalid) = CsvService.ParseRequests(rows);

        Assert.Empty(valid);
        Assert.Single(invalid);
    }

    [Fact]
    public void ParseRequests_MixedRows_SeparatesValidAndInvalid()
    {
        var rows = Rows(
            "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601",
            "R1001;C001;T-ECO;2025-06-15T11:20:00+02:00",
            "R1002;C001;;2025-06-16T10:00:00+02:00",   // empty tariff -> invalid
            "R1003;C002;T-BASIC;2025-06-17T09:00:00+02:00");

        var (valid, invalid) = CsvService.ParseRequests(rows);

        Assert.Equal(2, valid.Count);
        Assert.Single(invalid);
        Assert.Equal("R1002", invalid[0].RawId);
    }

    // ------- ParseTariffs -------

    [Fact]
    public void ParseTariffs_ReturnsEmptyList_ForHeaderOnly()
    {
        var rows = Rows("TariffId;Name;RequiresSmartMeter;BaseMonthlyGross");

        var result = CsvService.ParseTariffs(rows);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseTariffs_ParsesSingleValidRow()
    {
        var rows = Rows(
            "TariffId;Name;RequiresSmartMeter;BaseMonthlyGross",
            "T-ECO;OekoStrom;TRUE;29.90");

        var result = CsvService.ParseTariffs(rows);

        var tariff = Assert.Single(result);
        Assert.Equal("T-ECO", tariff.TariffId);
        Assert.Equal("OekoStrom", tariff.Name);
        Assert.True(tariff.RequiresSmartMeter);
        Assert.Equal(29.90m, tariff.BaseMonthlyGross);
    }

    [Fact]
    public void ParseTariffs_SkipsRows_WithTooFewColumns()
    {
        var rows = Rows(
            "TariffId;Name;RequiresSmartMeter;BaseMonthlyGross",
            "T-ECO;OekoStrom;TRUE"); // only 3 columns

        var result = CsvService.ParseTariffs(rows);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseTariffs_ParsesDecimal_WithDotSeparator()
    {
        var rows = Rows(
            "TariffId;Name;RequiresSmartMeter;BaseMonthlyGross",
            "T-PRO;ProFiX;FALSE;39.00");

        var result = CsvService.ParseTariffs(rows);

        Assert.Equal(39.00m, result[0].BaseMonthlyGross);
    }

    // Helper: splits each string by ';' to simulate CsvService row format
    private static List<string[]> Rows(params string[] lines) =>
        lines.Select(l => l.Split(';')).ToList();
}

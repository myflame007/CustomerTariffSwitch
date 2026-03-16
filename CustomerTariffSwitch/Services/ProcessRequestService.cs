using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Services;

public class ProcessRequestService
{
    // Timezone-aware SLA computation: all deadlines are calculated in Europe/Vienna local time
    private static readonly TimeZoneInfo ViennaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    public int CalculateSlaHours(Customer customer, Tariff tariff)
    {
        // Scenario 1: Standard SLA -> 48h base
        // Scenario 2: Premium SLA  -> 24h base
        var baseHours = customer.Sla == SLALevel.Premium ? 24 : 48;

        // Scenario 3: Smart meter upgrade needed -> +12h on top of base
        var additionalUpgradeHours = NeedsSmartMeterUpgrade(customer, tariff) ? 12 : 0;

        return baseHours + additionalUpgradeHours;
    }

    // Scenario 3: Tariff requires smart meter and customer still has a classic meter
    private static bool NeedsSmartMeterUpgrade(Customer customer, Tariff tariff) =>
        tariff.RequiresSmartMeter && customer.MeterType == MeterType.Classic;

    // IReadOnlyCollection statt List: der Service braucht nur Lesezugriff
    public List<RequestDecision> ProcessRequests(IReadOnlyCollection<Customer> customers, IReadOnlyCollection<SwitchRequest> requests,
            IReadOnlyCollection<Tariff> tariffs, IReadOnlyCollection<(string RawId, string Reason)> invalidRequests)
    {
        var customersById = customers.ToDictionary(c => c.CustomerId, StringComparer.OrdinalIgnoreCase);
        var tariffsById = tariffs.ToDictionary(t => t.TariffId, StringComparer.OrdinalIgnoreCase);

        var decisions = new List<RequestDecision>();

        // Scenario 7: rows that could not be parsed are rejected immediately
        foreach (var (rawId, reason) in invalidRequests)
            decisions.Add(RequestDecision.Rejected(rawId, reason));

        foreach (var request in requests)
        {
            // Scenario 5: Unknown customer ID
            if (!customersById.TryGetValue(request.CustomerId, out var customer))
            {
                decisions.Add(RequestDecision.Rejected(request.RequestId, "Unknown customer"));
                continue;
            }

            // Scenario 6: Unknown tariff ID
            if (!tariffsById.TryGetValue(request.TargetTariffId, out var tariff))
            {
                decisions.Add(RequestDecision.Rejected(request.RequestId, "Unknown tariff"));
                continue;
            }

            // Scenario 4: Customer has unpaid invoices
            if (customer.HasUnpaidInvoice)
            {
                decisions.Add(RequestDecision.Rejected(request.RequestId, "Unpaid invoice", customer.Name));
                continue;
            }

            var totalHours = CalculateSlaHours(customer, tariff);

            // SLA due time computed in Europe/Vienna local time 
            var dueAt = AddHoursInViennaLocalTime(request.RequestedAt, totalHours);
            var requiresUpdate = NeedsSmartMeterUpgrade(customer, tariff);
            var followUpAction = requiresUpdate ? "Schedule meter upgrade" : null;

            decisions.Add(RequestDecision.Approved(request.RequestId, customer.Name, dueAt, followUpAction));
        }

        return decisions;
    }


    private static DateTimeOffset AddHoursInViennaLocalTime(DateTimeOffset timestamp, int hoursToAdd)
    {
        var targetLocal = AddHoursInLocalTime(timestamp, hoursToAdd);
        return ResolveViennaOffset(targetLocal);
    }

    private static DateTime AddHoursInLocalTime(DateTimeOffset timestamp, int hoursToAdd)
    {
        var localStart = TimeZoneInfo.ConvertTime(timestamp, ViennaTimeZone).DateTime;
        return localStart.AddHours(hoursToAdd);
    }

    private static DateTimeOffset ResolveViennaOffset(DateTime targetLocal)
    {
        while (ViennaTimeZone.IsInvalidTime(targetLocal))
        {
            targetLocal = targetLocal.AddMinutes(1);
        }
        //Explanation:
        //Die ganze Stunde 02:00 bis 02:59 existiert nicht: https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.isinvalidtime?view=net-10.0
        //29.03.2026 2:21 is invalid time 
        //01:59 CET->OK
        //02:00 CET->INVALID (existiert nicht)
        //02:30 CET->INVALID
        //02:59 CET->INVALID
        //03:00 CEST->OK

        if (ViennaTimeZone.IsAmbiguousTime(targetLocal))
        {
            var ambiguousOffsets = ViennaTimeZone.GetAmbiguousTimeOffsets(targetLocal);
            return new DateTimeOffset(targetLocal, ambiguousOffsets.Max());
        }

        return new DateTimeOffset(targetLocal, ViennaTimeZone.GetUtcOffset(targetLocal));
    }
}

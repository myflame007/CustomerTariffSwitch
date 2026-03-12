using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Services;

public class ProcessRequestService
{
    private static readonly TimeZoneInfo ViennaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    public int CalculateSlaHours(Customer customer, Tariff tariff)
    {
        var baseHours = customer.Sla == SLALevel.Premium ? 24 : 48;
        var additionalUpgradeHours = tariff.RequiresSmartMeter && customer.MeterType == MeterType.Classic ? 12 : 0;
        return baseHours + additionalUpgradeHours;
    }

    public List<RequestDecision> ProcessRequests(IReadOnlyCollection<Customer> customers, IReadOnlyCollection<SwitchRequest> requests, IReadOnlyCollection<Tariff> tariffs)
    {
        var customersById = customers.ToDictionary(c => c.CustomerId, StringComparer.OrdinalIgnoreCase);
        var tariffsById = tariffs.ToDictionary(t => t.TariffId, StringComparer.OrdinalIgnoreCase);

        var decisions = new List<RequestDecision>();

        foreach (var request in requests)
        {
            if (!customersById.TryGetValue(request.CustomerId, out var customer))
            {
                decisions.Add(RequestDecision.Rejected(request.RequestId, "Unknown customer"));
                continue;
            }

            if (!tariffsById.TryGetValue(request.TargetTariffId, out var tariff))
            {
                decisions.Add(RequestDecision.Rejected(request.RequestId, "Unknown tariff"));
                continue;
            }

            if (customer.HasUnpaidInvoice)
            {
                decisions.Add(RequestDecision.Rejected(request.RequestId, "Unpaid invoice"));
                continue;
            }

            var requiresUpgrade = tariff.RequiresSmartMeter && customer.MeterType == MeterType.Classic;
            var totalHours = CalculateSlaHours(customer, tariff);
            var dueAt = AddHoursInViennaLocalTime(request.RequestedAt, totalHours);
            var followUpAction = requiresUpgrade ? "Schedule meter upgrade" : null;

            decisions.Add(RequestDecision.Approved(request.RequestId, dueAt, followUpAction));
        }

        return decisions;
    }

    private static DateTimeOffset AddHoursInViennaLocalTime(DateTimeOffset timestamp, int hoursToAdd)
    {
        var localStart = TimeZoneInfo.ConvertTime(timestamp, ViennaTimeZone).DateTime;
        var targetLocal = localStart.AddHours(hoursToAdd);

        while (ViennaTimeZone.IsInvalidTime(targetLocal))
        {
            targetLocal = targetLocal.AddMinutes(1);
        }

        if (ViennaTimeZone.IsAmbiguousTime(targetLocal))
        {
            var ambiguousOffsets = ViennaTimeZone.GetAmbiguousTimeOffsets(targetLocal);
            return new DateTimeOffset(targetLocal, ambiguousOffsets.Max());
        }

        return new DateTimeOffset(targetLocal, ViennaTimeZone.GetUtcOffset(targetLocal));
    }
}

public class RequestDecision
{
    public required string RequestId { get; init; }
    public required string Status { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public string? FollowUpAction { get; init; }

    public static RequestDecision Approved(string requestId, DateTimeOffset dueAt, string? followUpAction = null) =>
        new()
        {
            RequestId = requestId,
            Status = "Approved",
            DueAt = dueAt,
            FollowUpAction = followUpAction
        };

    public static RequestDecision Rejected(string requestId, string reason) =>
        new()
        {
            RequestId = requestId,
            Status = "Rejected",
            Reason = reason,
            DueAt = null,
            FollowUpAction = null
        };
}

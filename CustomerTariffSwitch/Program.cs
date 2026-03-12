using CustomerTariffSwitch.Services;

var csvService = new CsvService();
var processRequestService = new ProcessRequestService();

Console.WriteLine("Reading CSV ...");
var (customers, requests, tariffs) = csvService.ReadKnownFiles();

Console.WriteLine("Calculating SLA hours ...");
var customersById = customers.ToDictionary(c => c.CustomerId, StringComparer.OrdinalIgnoreCase);
var tariffsById = tariffs.ToDictionary(t => t.TariffId, StringComparer.OrdinalIgnoreCase);

foreach (var request in requests)
{
    if (!customersById.TryGetValue(request.CustomerId, out var customer))
    {
        Console.WriteLine($"{request.RequestId} | SLA-Hours=N/A | Unknown customer");
        continue;
    }

    if (!tariffsById.TryGetValue(request.TargetTariffId, out var tariff))
    {
        Console.WriteLine($"{request.RequestId} | SLA-Hours=N/A | Unknown tariff");
        continue;
    }

    if (customer.HasUnpaidInvoice)
    {
        Console.WriteLine($"{request.RequestId} | SLA-Hours=N/A | Unpaid invoice");
        continue;
    }

    var slaHours = processRequestService.CalculateSlaHours(customer, tariff);
    Console.WriteLine($"{request.RequestId} | SLA-Hours={slaHours}");
}

Console.WriteLine("Processing requests ...");
var decisions = processRequestService.ProcessRequests(customers, requests, tariffs);

foreach (var decision in decisions)
{
    var reasonPart = string.IsNullOrWhiteSpace(decision.Reason) ? string.Empty : $" | {decision.Reason}";
    var dueAtPart = decision.DueAt.HasValue ? $" | DueAt={decision.DueAt.Value:O}" : string.Empty;
    var followUpPart = string.IsNullOrWhiteSpace(decision.FollowUpAction) ? string.Empty : $" | Action={decision.FollowUpAction}";
    Console.WriteLine($"{decision.RequestId} | {decision.Status}{reasonPart}{dueAtPart}{followUpPart}");
}

Console.WriteLine();

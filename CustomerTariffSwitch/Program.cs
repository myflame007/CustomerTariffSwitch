using CustomerTariffSwitch.Models;
using CustomerTariffSwitch.Services;

var csvService = new CsvService();
var processRequestService = new ProcessRequestService();
var decisionRepository = new DecisionRepository();

Console.WriteLine("=== CustomerTariffSwitch ===");
Console.WriteLine();

Console.WriteLine("Loading CSV files ...");
var (customers, requests, tariffs, invalidRequests) = csvService.ReadKnownFiles();
Console.WriteLine($"  => {customers.Count} customers, {tariffs.Count} tariffs, {requests.Count} requests loaded");
Console.WriteLine();

// Scenario 8: skip requests that were already processed in a previous run
var processedIds = decisionRepository.LoadProcessedRequestIds();
var skippedCount = requests.Count(r => processedIds.Contains(r.RequestId));

// collection expression: [.. x] == x.ToList()
requests = [.. requests.Where(r => !processedIds.Contains(r.RequestId))];

if (skippedCount > 0)
    Console.WriteLine($"  => {skippedCount} request(s) skipped (already processed in a previous run)");

if (requests.Count == 0 && invalidRequests.Count == 0)
{
    Console.WriteLine("  => Nothing to process -- all requests have already been handled.");
    Console.WriteLine();
    Console.WriteLine("  To reprocess from scratch, delete the Output/ folder and re-run.");
    return;
}

Console.WriteLine("Processing requests ...");
Console.WriteLine(new string('-', 60));

var decisions = processRequestService.ProcessRequests(customers, requests, tariffs, invalidRequests);

foreach (var decision in decisions)
{
    Console.WriteLine($"  {decision}");
}

Console.WriteLine(new string('-', 60));
var approvedCount = decisions.Count(d => d.Status == DecisionStatus.Approved);
var rejectedCount = decisions.Count(d => d.Status == DecisionStatus.Rejected);
Console.WriteLine($"  => {approvedCount} approved, {rejectedCount} rejected");
Console.WriteLine();

// Persist all decisions (including follow-up actions + deadlines) to Output/decisions.json
decisionRepository.AppendDecisions(decisions);

var outputPath = decisionRepository.GetOutputFilePath();
Console.WriteLine($"  => Decisions saved to: {outputPath}");

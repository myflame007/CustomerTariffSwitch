using CustomerTariffSwitch.Models;
using CustomerTariffSwitch.Services;

const string decisionsOutputPath = "Output/decisions.json";

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

// collection expression: [.. requests.Where(r => !processedIds.Contains(r.RequestId))] == requests.Where(r => !processedIds.Contains(r.RequestId)).ToList();
requests = [.. requests.Where(r => !processedIds.Contains(r.RequestId))]; 

Console.WriteLine("Processing requests ...");
Console.WriteLine(new string('-', 60));

var decisions = processRequestService.ProcessRequests(customers, requests, tariffs, invalidRequests);

foreach (var decision in decisions)
{
    Console.WriteLine(decision);
}

Console.WriteLine(new string('-', 60));
Console.WriteLine($"  => {decisions.Count(d => d.Status == DecisionStatus.Approved)} approved, {decisions.Count(d => d.Status == DecisionStatus.Rejected)} rejected");
Console.WriteLine();

// Persist all decisions (including follow-up actions + deadlines) to Output/decisions.json
decisionRepository.AppendDecisions(decisions);
Console.WriteLine($"  => Decisions saved to {decisionsOutputPath}");

using CustomerTariffSwitch.Services;

var csvService = new CsvService();
var csvFiles = csvService.ReadAllSolutionItemCsvFiles();

foreach (var file in csvFiles)
{
    Console.WriteLine($"=== {file.Key} ({file.Value.Count} rows) ===");

    foreach (var row in file.Value)
    {
        Console.WriteLine(string.Join(" | ", row));
    }

    Console.WriteLine();
}

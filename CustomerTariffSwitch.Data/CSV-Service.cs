using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Services;

public class CsvService
{
    private const string InputFolderName = "Input Files";

    public (List<Customer> Customers, List<SwitchRequest> Requests, List<Tariff> Tariffs, List<(string RawId, string Reason)> InvalidRequests) ReadKnownFiles()
    {
        var all = ReadAllSolutionItemCsvFiles();

        // Fail-fast: if any required file is missing, throw with an actionable message
        // so the user knows exactly which file to add before re-running
        foreach (var required in new[] { "customers.csv", "requests.csv", "tariffs.csv" })
        {
            if (!all.ContainsKey(required))
                throw new FileNotFoundException($"Required file '{required}' not found in the Input Files folder.");
        }

        var (validRequests, invalidRequests) = ParseRequests(all["requests.csv"]);

        return (
            ParseCustomers(all["customers.csv"]),
            validRequests,
            ParseTariffs(all["tariffs.csv"]),
            invalidRequests
        );
    }

    public Dictionary<string, List<string[]>> ReadAllSolutionItemCsvFiles()
    {
        var inputDirectory = FindInputDirectory();
        var csvFiles = Directory.GetFiles(inputDirectory, "*.csv", SearchOption.TopDirectoryOnly);

        if (csvFiles.Length == 0)
        {
            throw new InvalidOperationException($"No CSV files found in '{inputDirectory}'.");
        }

        var result = new ConcurrentDictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);

        Parallel.ForEach(csvFiles, csvFilePath =>
        {
            var fileName = Path.GetFileName(csvFilePath);
            Console.WriteLine($"  Reading {fileName} ...");

            var lines = ReadAllLinesWithSharedAccess(csvFilePath);
            var rows = new List<string[]>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                rows.Add(line.Split(';'));
            }

            result[fileName] = rows;
        });

        return new Dictionary<string, List<string[]>>(result, StringComparer.OrdinalIgnoreCase);
    }

    private static List<Customer> ParseCustomers(List<string[]> rows)
    {
        return rows
            .Skip(1) // header
            .Where(r => r.Length >= 5)
            .Select(r => new Customer
            {
                CustomerId = r[0],
                Name = r[1],
                HasUnpaidInvoice = bool.Parse(r[2]),
                Sla = ParseEnum<SLALevel>(r[3], "SLA"),
                MeterType = ParseEnum<MeterType>(r[4], "MeterType")
            })
            .ToList();
    }

    private static (List<SwitchRequest> Valid, List<(string RawId, string Reason)> Invalid) ParseRequests(List<string[]> rows)
    {
        var valid = new List<SwitchRequest>();
        var invalid = new List<(string RawId, string Reason)>();

        foreach (var r in rows.Skip(1)) // header
        {
            var rawId = r.Length > 0 ? r[0] : "unknown";

            try
            {
                // Reject rows with too few columns or any empty required field
                if (r.Length < 4 || r.Take(4).Any(field => string.IsNullOrWhiteSpace(field)))
                {
                    invalid.Add((rawId, "Invalid request data"));
                    continue;
                }

                valid.Add(new SwitchRequest
                {
                    RequestId = r[0],
                    CustomerId = r[1],
                    TargetTariffId = r[2],
                    RequestedAt = DateTimeOffset.Parse(r[3], CultureInfo.InvariantCulture)
                });
            }
            catch
            {
                // Catches malformed timestamps or any other unexpected parse error
                invalid.Add((rawId, "Invalid request data"));
            }
        }

        return (valid, invalid);
    }

    private static List<Tariff> ParseTariffs(List<string[]> rows)
    {
        return rows
            .Skip(1) // header
            .Where(r => r.Length >= 4)
            .Select(r => new Tariff
            {
                TariffId = r[0],
                Name = r[1],
                RequiresSmartMeter = bool.Parse(r[2]),
                BaseMonthlyGross = decimal.Parse(r[3], CultureInfo.InvariantCulture)
            })
            .ToList();
    }


    private static TEnum ParseEnum<TEnum>(string rawValue, string fieldName) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(rawValue, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new FormatException($"Invalid value '{rawValue}' for {fieldName}.");
    }

    private static List<string> ReadAllLinesWithSharedAccess(string path)
    {
        var lines = new List<string>();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            lines.Add(reader.ReadLine() ?? string.Empty);
        }

        return lines;
    }

    private static string FindInputDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, InputFolderName);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find '{InputFolderName}' folder by walking up from '{AppContext.BaseDirectory}'.");
    }
}


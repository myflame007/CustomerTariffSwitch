using System.Globalization;
using System.Text;
using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Services;

public class CsvService
{
    private const string InputFolderName = "Input Files";

    public (List<Customer> Customers, List<SwitchRequest> Requests, List<Tariff> Tariffs) ReadKnownFiles()
    {
        var all = ReadAllSolutionItemCsvFiles();
        return (
            ParseCustomers(all["customers.csv"]),
            ParseRequests(all["requests.csv"]),
            ParseTariffs(all["tariffs.csv"])
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

        var result = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);

        foreach (var csvFilePath in csvFiles)
        {
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

            result[Path.GetFileName(csvFilePath)] = rows;
        }

        return result;
    }

    private static List<Customer> ParseCustomers(List<string[]> rows)
    {
        return rows
            .Skip(1) // header
            .Where(r => r.Length >= 5)
            .Select(r => new Customer
            {
                CustomerId = r[0],
                Name = FixBrokenEncoding(r[1]),
                HasUnpaidInvoice = bool.Parse(r[2]),
                Sla = ParseEnum<SLALevel>(r[3], "SLA"),
                MeterType = ParseEnum<MeterType>(r[4], "MeterType")
            })
            .ToList();
    }

    private static List<SwitchRequest> ParseRequests(List<string[]> rows)
    {
        return rows
            .Skip(1) // header
            .Where(r => r.Length >= 4)
            .Select(r => new SwitchRequest
            {
                RequestId = r[0],
                CustomerId = r[1],
                TargetTariffId = r[2],
                RequestedAt = DateTimeOffset.Parse(r[3], CultureInfo.InvariantCulture)
            })
            .ToList();
    }

    private static List<Tariff> ParseTariffs(List<string[]> rows)
    {
        return rows
            .Skip(1) // header
            .Where(r => r.Length >= 4)
            .Select(r => new Tariff
            {
                TariffId = r[0],
                Name = FixBrokenEncoding(r[1]),
                RequiresSmartMeter = bool.Parse(r[2]),
                BaseMonthlyGross = decimal.Parse(r[3], CultureInfo.InvariantCulture)
            })
            .ToList();
    }

    private static string FixBrokenEncoding(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!value.Contains('Ã') && !value.Contains('Â'))
        {
            return value;
        }

        var latin1Bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(value);
        return Encoding.UTF8.GetString(latin1Bytes);
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


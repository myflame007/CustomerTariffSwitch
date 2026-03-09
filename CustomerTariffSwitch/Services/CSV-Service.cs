namespace CustomerTariffSwitch.Services;

internal class CsvService
{
    private const string InputFolderName = "Input Files";

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

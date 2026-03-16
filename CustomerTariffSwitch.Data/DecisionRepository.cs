using System.Text.Json;
using System.Text.Json.Serialization;
using CustomerTariffSwitch.Data.Helper;
using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Services;

public class DecisionRepository
{
    private const string OutputFolderName = "Output";
    private const string DecisionsFileName = "decisions.json";

    private readonly string? _overridePath;

    // Production: uses SolutionPathHelper to locate Output/decisions.json
    // Tests: pass an explicit path to a temp directory
    public DecisionRepository(string? overridePath = null)
    {
        _overridePath = overridePath;
    }

    // Ignore when empty
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public IReadOnlySet<string> LoadProcessedRequestIds()
    {
        var path = GetDecisionsFilePath();

        if (!File.Exists(path))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(path);
        var existing = JsonSerializer.Deserialize<List<RequestDecision>>(json, JsonOptions) ?? [];

        return existing
            .Select(d => d.RequestId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Appends new decisions to the output file (creates it if it doesn't exist)
    // Idempotent: decisions already persisted (by RequestId) are skipped
    // Atomic write via temp-file swap prevents partial/corrupt output on crash
    public void AppendDecisions(IEnumerable<RequestDecision> newDecisions)
    {
        var path = GetDecisionsFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var existing = new List<RequestDecision>();

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            existing = JsonSerializer.Deserialize<List<RequestDecision>>(json, JsonOptions) ?? [];
        }

        var existingIds = existing
            .Select(d => d.RequestId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        existing.AddRange(newDecisions.Where(d => !existingIds.Contains(d.RequestId)));

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(existing, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    private string GetDecisionsFilePath()
    {
        if (_overridePath != null)
            return _overridePath;

        var root = SolutionPathHelper.FindRootByMarker("Input Files");
        return Path.Combine(root, OutputFolderName, DecisionsFileName);
    }
}





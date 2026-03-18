using CustomerTariffSwitch.Data.Services;
using CustomerTariffSwitch.Models;

namespace CustomerTariffSwitch.Test;

public class DecisionRepositoryTests : IDisposable
{
    private readonly string _tempFile;
    private readonly DecisionRepository _decisionRepository;

    public DecisionRepositoryTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"decisions_{Guid.NewGuid():N}.json");
        _decisionRepository = new DecisionRepository(overridePath: _tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
        if (File.Exists(_tempFile + ".tmp")) File.Delete(_tempFile + ".tmp");
    }

    // --- LoadProcessedRequestIds ---

    [Fact]
    public void LoadProcessedRequestIds_ReturnsEmptySet_WhenFileDoesNotExist()
    {
        var ids = _decisionRepository.LoadProcessedRequestIds();

        Assert.Empty(ids);
    }

    [Fact]
    public void LoadProcessedRequestIds_ReturnsIds_WhenFileExists()
    {
        var decisions = new List<RequestDecision>
        {
            RequestDecision.Approved("R-1", "Anna", DateTimeOffset.UtcNow),
            RequestDecision.Rejected("R-2", "Unknown customer")
        };
        _decisionRepository.AppendDecisions(decisions);

        var ids = _decisionRepository.LoadProcessedRequestIds();

        Assert.Contains("R-1", ids);
        Assert.Contains("R-2", ids);
    }

    [Fact]
    public void LoadProcessedRequestIds_IsCaseInsensitive()
    {
        _decisionRepository.AppendDecisions([RequestDecision.Approved("r-abc", "Test", DateTimeOffset.UtcNow)]);

        var ids = _decisionRepository.LoadProcessedRequestIds();

        Assert.Contains("R-ABC", ids);
        Assert.Contains("r-abc", ids);
    }

    // --- AppendDecisions ---

    [Fact]
    public void AppendDecisions_CreatesFile_WhenItDoesNotExist()
    {
        _decisionRepository.AppendDecisions([RequestDecision.Approved("R-1", "Anna", DateTimeOffset.UtcNow)]);

        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public void AppendDecisions_WritesValidJson()
    {
        _decisionRepository.AppendDecisions([RequestDecision.Approved("R-1", "Anna", DateTimeOffset.UtcNow)]);

        var json = File.ReadAllText(_tempFile);
        Assert.StartsWith("[", json.TrimStart());
    }

    [Fact]
    public void AppendDecisions_AccumulatesDecisionsAcrossCalls()
    {
        _decisionRepository.AppendDecisions([RequestDecision.Approved("R-1", "Anna", DateTimeOffset.UtcNow)]);
        _decisionRepository.AppendDecisions([RequestDecision.Rejected("R-2", "Unknown tariff")]);

        var ids = _decisionRepository.LoadProcessedRequestIds();

        Assert.Equal(2, ids.Count);
        Assert.Contains("R-1", ids);
        Assert.Contains("R-2", ids);
    }

    [Fact]
    public void AppendDecisions_IsIdempotent_DuplicateRequestIdIsNotWrittenTwice()
    {
        // Scenario 8: same RequestId appended twice must not result in duplicate entry
        var decision = RequestDecision.Approved("R-1", "Anna", DateTimeOffset.UtcNow);
        _decisionRepository.AppendDecisions([decision]);
        _decisionRepository.AppendDecisions([decision]);

        var ids = _decisionRepository.LoadProcessedRequestIds();

        Assert.Single(ids);
    }

    [Fact]
    public void AppendDecisions_PersistsFollowUpAction_WithDeadline()
    {
        var dueAt = DateTimeOffset.Parse("2025-06-04T00:00:00+02:00");
        var decision = RequestDecision.Approved("R-1", "Anna", dueAt, followUpAction: "Schedule meter upgrade");
        _decisionRepository.AppendDecisions([decision]);

        var json = File.ReadAllText(_tempFile);

        Assert.Contains("Schedule meter upgrade", json);
        Assert.Contains("2025-06-04", json);
    }

    [Fact]
    public void AppendDecisions_LeavesNoTempFile_AfterSuccessfulWrite()
    {
        _decisionRepository.AppendDecisions([RequestDecision.Approved("R-1", "Anna", DateTimeOffset.UtcNow)]);

        Assert.False(File.Exists(_tempFile + ".tmp"));
    }
}

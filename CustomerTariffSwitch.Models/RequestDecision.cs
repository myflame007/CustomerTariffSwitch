namespace CustomerTariffSwitch.Models;

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

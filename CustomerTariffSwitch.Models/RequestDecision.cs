namespace CustomerTariffSwitch.Models;

public class RequestDecision
{
    public required string RequestId { get; init; }
    public required DecisionStatus Status { get; init; }
    public string? CustomerName { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public string? FollowUpAction { get; init; }
    public DateTimeOffset? FollowUpDueAt { get; init; }

    public static RequestDecision Approved(string requestId, string customerName, DateTimeOffset dueAt, string? followUpAction = null) =>
        new()
        {
            RequestId = requestId,
            Status = DecisionStatus.Approved,
            CustomerName = customerName,
            DueAt = dueAt,
            FollowUpAction = followUpAction,
            FollowUpDueAt = followUpAction != null ? dueAt : null
        };

    public static RequestDecision Rejected(string requestId, string reason, string? customerName = null) =>
        new()
        {
            RequestId = requestId,
            Status = DecisionStatus.Rejected,
            CustomerName = customerName,
            Reason = reason,
            DueAt = null,
            FollowUpAction = null
        };

    public override string ToString()
    {
        var label        = Status == DecisionStatus.Approved ? "[APPROVED]" : "[REJECTED]";
        var customerPart = string.IsNullOrWhiteSpace(CustomerName)   ? string.Empty : $" | {CustomerName}";
        var reasonPart   = string.IsNullOrWhiteSpace(Reason)         ? string.Empty : $" | Reason: {Reason}";
        var dueAtPart    = DueAt.HasValue                            ? $" | Due: {DueAt.Value:O}" : string.Empty;
        var followUpPart = string.IsNullOrWhiteSpace(FollowUpAction) ? string.Empty : $" | Follow-up: {FollowUpAction}";
        return $"{label} {RequestId}{customerPart}{reasonPart}{dueAtPart}{followUpPart}";
    }
}

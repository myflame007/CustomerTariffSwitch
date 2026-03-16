namespace CustomerTariffSwitch.Models;

public class RequestDecision
{
    public required string RequestId { get; init; }
    public required DecisionStatus Status { get; init; }
    public string? CustomerName { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public string? FollowUpAction { get; init; }

    public static RequestDecision Approved(string requestId, string customerName, DateTimeOffset dueAt, string? followUpAction = null) =>
        new()
        {
            RequestId = requestId,
            Status = DecisionStatus.Approved,
            CustomerName = customerName,
            DueAt = dueAt,
            FollowUpAction = followUpAction
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
        var customerPart   = string.IsNullOrWhiteSpace(CustomerName)  ? string.Empty : $" | {CustomerName}";
        var reasonPart     = string.IsNullOrWhiteSpace(Reason)        ? string.Empty : $" | {Reason}";
        var dueAtPart      = DueAt.HasValue                           ? $" | DueAt={DueAt.Value:O}" : string.Empty;
        var followUpPart   = string.IsNullOrWhiteSpace(FollowUpAction) ? string.Empty : $" | Action={FollowUpAction}";
        return $"{RequestId} | {Status.ToString()}{customerPart}{reasonPart}{dueAtPart}{followUpPart}";
    }
}

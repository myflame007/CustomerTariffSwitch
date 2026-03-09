using System;
using System.Collections.Generic;
using System.Text;

namespace CustomerTariffSwitch.Models
{
    public class RequestDecision
    {
        public required string RequestId { get; init; }
        public required string Status { get; init; }
        public string? Reason { get; init; }

        public static RequestDecision Approved(string requestId) =>
            new()
            {
                RequestId = requestId,
                Status = "Approved"
            };

        public static RequestDecision Rejected(string requestId, string reason) =>
            new()
            {
                RequestId = requestId,
                Status = "Rejected",
                Reason = reason
            };
    }
}

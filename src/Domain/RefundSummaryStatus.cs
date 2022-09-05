using System.Collections.Generic;

namespace Domain
{
    public static class RefundSummaryStatus
    {
        public const string Pending = "pending";
        public const string Unavailable = "unavailable";
        public const string Available = "available";
        public const string Full = "full";

        public static string ToReason(this string state)
        {
            var reasons = new Dictionary<string, string>()
            {
                { Pending, "You cannot refund the payment yet because your user has not completed the payment" },
                { Unavailable, "You cannot refund the payment, usually because the payment failed" },
                { Available, "You can refund the payment" },
                { Full, "ou cannot refund the payment because you’ve already refunded the full amount" },
            };

            if(!reasons.ContainsKey(state))
                return "Unknown reason";

            return reasons[state];
        }
    }
}

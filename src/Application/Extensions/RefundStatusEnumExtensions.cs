using Domain;
using GovUKPayApiClient.Model;
using System.Collections.Generic;

namespace Application.Extensions
{
    public static class RefundStatusEnumExtensions
    {
        public static bool IsFinished(this Refund.StatusEnum? status)
        {
            if (!status.HasValue) return false;

            // More info here: https://docs.payments.service.gov.uk/refunding_payments/#checking-the-status-of-a-refund
            // TODO: Extract these strings into enum style classes
            var mappings = new Dictionary<Refund.StatusEnum, bool>()
            {
                { Refund.StatusEnum.Submitted, false },
                { Refund.StatusEnum.Success, true },
                { Refund.StatusEnum.Error, true }
            };

            if (!mappings.ContainsKey(status.Value)) return false;

            return mappings[status.Value];
        }

        public static string ToAuthResult(this Refund.StatusEnum? status)
        {
            if (!status.HasValue) return AuthResult.Error;

            // More info here: https://docs.payments.service.gov.uk/refunding_payments/#checking-the-status-of-a-refund
            // TODO: Extract these strings into enum style classes
            var authResultMappings = new Dictionary<Refund.StatusEnum, string>()
            {
                { Refund.StatusEnum.Submitted, AuthResult.Pending },
                { Refund.StatusEnum.Success, AuthResult.Authorised },
                { Refund.StatusEnum.Error, AuthResult.Error }
            };
            
            if (!authResultMappings.ContainsKey(status.Value)) return AuthResult.Error;

            return authResultMappings[status.Value];
        }
    }
}

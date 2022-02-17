using GovUKPayApiClient.Model;
using System.Collections.Generic;

namespace Application.Extensions
{
    public static class PaymentStateExtensions
    {
        public static string GetAuthResult(this PaymentState source)
        {
            // More info here: https://docs.payments.service.gov.uk/api_reference/#status-and-finished
            // TODO: Extract these strings into enum style classes
            var authResultMappings = new Dictionary<string, string>()
            {
                { "created", "PENDING" },
                { "started", "PENDING" },
                { "submitted", "PENDING" },
                { "capturable", "PENDING" },
                { "success", "AUTHORISED" },
                { "failed", "REFUSED" },
                { "cancelled", "ERROR" },
                { "error", "ERROR" }
            };

            return authResultMappings[source.Status];
        }

    }
}

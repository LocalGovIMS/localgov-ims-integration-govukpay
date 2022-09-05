using Domain;
using GovUKPayApiClient.Model;
using System.Collections.Generic;

namespace Application.Extensions
{
    public static class PaymentStateExtensions
    {
        public static string ToAuthResult(this PaymentState source)
        {
            // More info here: https://docs.payments.service.gov.uk/api_reference/#status-and-finished
            // TODO: Extract these strings into enum style classes
            var authResultMappings = new Dictionary<string, string>()
            {
                { PaymentStatus.Created, AuthResult.Pending },
                { PaymentStatus.Started, AuthResult.Pending },
                { PaymentStatus.Submitted, AuthResult.Pending },
                { PaymentStatus.Capturable, AuthResult.Pending },
                { PaymentStatus.Success, AuthResult.Authorised },
                { PaymentStatus.Failed, AuthResult.Refused },
                { PaymentStatus.Cancelled, AuthResult.Error },
                { PaymentStatus.Error, AuthResult.Error }
            };

            return authResultMappings[source.Status];
        }
    }
}

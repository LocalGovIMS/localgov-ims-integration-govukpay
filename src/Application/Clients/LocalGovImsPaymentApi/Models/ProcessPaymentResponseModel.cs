using System.Diagnostics.CodeAnalysis;

namespace Application.Clients.LocalGovImsPaymentApi
{
    [ExcludeFromCodeCoverage]
    public class ProcessPaymentResponseModel
    {
        public string RedirectUrl { get; set; }
        public bool Success { get; set; }
    }
}

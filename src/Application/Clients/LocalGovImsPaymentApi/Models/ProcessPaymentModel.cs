using System.Diagnostics.CodeAnalysis;

namespace Application.Clients.LocalGovImsPaymentApi
{
    [ExcludeFromCodeCoverage]
    public class ProcessPaymentModel
    {
        public string AuthResult { get; set; }
        public string PspReference { get; set; }
        public string MerchantReference { get; set; }
        public string PaymentMethod { get; set; }
    }
}

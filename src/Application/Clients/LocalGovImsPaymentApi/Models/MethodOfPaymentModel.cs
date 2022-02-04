using System.Diagnostics.CodeAnalysis;

namespace Application.Clients.LocalGovImsPaymentApi
{
    [ExcludeFromCodeCoverage]
    public class MethodOfPaymentModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal MaximumAmount { get; set; }
        public decimal MinimumAmount { get; set; }
    }
}

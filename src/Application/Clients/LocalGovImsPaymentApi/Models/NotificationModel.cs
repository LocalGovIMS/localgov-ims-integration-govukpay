using System;
using System.Diagnostics.CodeAnalysis;

namespace Application.Clients.LocalGovImsPaymentApi
{
    [ExcludeFromCodeCoverage]
    public class NotificationModel
    {
        public string MerchantReference { get; set; }
        public string EventCode { get; set; }
        public string OriginalReference { get; set; }
        public string PspReference { get; set; }
        public DateTime? EventDate { get; set; }
        public string PaymentMethod { get; set; }
        public bool Success { get; set; }
        public string Reason { get; set; }
        public string Operations { get; set; }
        public bool Live { get; set; }
        public bool Processed { get; set; }

        public string MerchantAccountCode { get; set; }
        public decimal? Amount { get; set; }
    }
}

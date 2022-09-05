using System;
using System.Diagnostics.CodeAnalysis;

namespace Application.Commands
{
    [ExcludeFromCodeCoverage]
    public class RefundModel
    {
        // IMS ProcessedTransaction.PspReference of original payment - which is the Payment.PaymentId in this integration
        public string Reference { get; set; } 

        // IMS PendingTransaction.InternalReference of refund the transations (there may be more than one, and they're grouped using this value)
        public string ImsReference { get; set; } 

        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}

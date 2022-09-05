using System;

namespace Web.Controllers
{
    public class RefundModel
    {
        public string Reference { get; set; }
        public string ImsReference { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}

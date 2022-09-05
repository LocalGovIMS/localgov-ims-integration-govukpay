using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Application.Entities
{
    [ExcludeFromCodeCoverage]
    public class Refund : BaseEntity
    {
        public Refund()
        {
            StatusHistory = new HashSet<RefundStatusHistory>();
        }

        public DateTime CreatedDate { get; set; }
        
        public Guid Identifier { get; set; }

        [StringLength(36)]
        public string RefundReference { get; set; }

        [StringLength(36)]
        public string PaymentReference { get; set; }

        public decimal Amount { get; set; }

        [StringLength(255)]
        public string PaymentId { get; set; }

        [StringLength(255)]
        public string RefundId { get; set; }

        [StringLength(100)]
        public string Status { get; set; }

        public bool Finished { get; set; }

        public virtual ICollection<RefundStatusHistory> StatusHistory { get; set; }
    }
}

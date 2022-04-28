using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.Entities
{
    public class Payment : BaseEntity
    {
        public Payment()
        {
            StatusHistory = new HashSet<PaymentStatusHistory>();
        }

        public DateTime CreatedDate { get; set; }
        
        public Guid Identifier { get; set; }

        [StringLength(36)]
        public string Reference { get; set; }

        public decimal Amount { get; set; }

        [StringLength(255)]
        public string PaymentId { get; set; }

        [StringLength(100)]
        public string NextUrl { get; set; }

        [StringLength(100)]
        public string Status { get; set; }

        public bool Finished { get; set; }

        public DateTime? CapturedDate { get; set; }

        [StringLength(255)]
        public string FailureUrl { get; set; }

        public virtual ICollection<PaymentStatusHistory> StatusHistory { get; set; }
    }
}

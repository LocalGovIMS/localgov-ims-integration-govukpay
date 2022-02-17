using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Entities
{
    public class PaymentStatusHistory : BaseEntity
    {
        public DateTime CreatedDate { get; set; }
        
        public int PaymentId { get; set; }
        public Payment Payment { get; set; }

        [StringLength(5)]
        public string Code { get; set; }

        public bool Finished { get; set; }
        public string Message { get; set; }

        [StringLength(100)]
        public string Status { get; set; }
    }
}

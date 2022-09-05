using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Application.Entities
{
    [ExcludeFromCodeCoverage]
    public class RefundStatusHistory : BaseEntity
    {
        public DateTime CreatedDate { get; set; }
        
        public int RefundId { get; set; }
        public Refund Refund { get; set; }

        [StringLength(5)]
        public string Code { get; set; }

        public bool Finished { get; set; }
        public string Message { get; set; }

        [StringLength(100)]
        public string Status { get; set; }
    }
}

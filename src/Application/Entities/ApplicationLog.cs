using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Application.Entities
{
    [ExcludeFromCodeCoverage]
    public class ApplicationLog : BaseEntity
    {
        public string Message { get; set; }
        public string MessageTemplate { get; set; }

        [StringLength(128)]
        public string Level { get; set; }

        [Required]
        public DateTime TimeStamp { get; set; }

        public string Exception { get; set; }
        public string Properties { get; set; }
    }
}

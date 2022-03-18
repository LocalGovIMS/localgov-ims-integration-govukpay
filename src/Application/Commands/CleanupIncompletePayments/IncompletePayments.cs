using Application.Data;
using System;

namespace Application.Commands
{
    public sealed class IncompletePayments : BaseSpecification<Entities.Payment>
    {
        public IncompletePayments(DateTime thresholdDate) 
            : base(x => x.PaymentId != null
                && x.Finished == false
                && x.CreatedDate <= thresholdDate)
        {
            AddInclude(x => x.StatusHistory);
            AddOrder(x => x.CreatedDate);
        }
    }
}

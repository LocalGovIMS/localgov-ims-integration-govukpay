using Application.Data;
using Domain;

namespace Application.Commands
{
    public sealed class UncapturedPayments : BaseSpecification<Entities.Payment>
    {
        public UncapturedPayments(int batchSize) 
            : base(x => x.PaymentId != null
                && x.Finished == true
                && x.Status == PaymentStatus.Success
                && x.CapturedDate == null)
        {
            AddInclude(x => x.StatusHistory);
            AddOrder(x => x.CreatedDate);
            AddCount(batchSize);
        }
    }
}

using Application.Data;
using Application.Extensions;
using static GovUKPayApiClient.Model.Refund;

namespace Application.Commands
{
    public sealed class IncompleteRefunds : BaseSpecification<Entities.Refund>
    {
        public IncompleteRefunds(int batchSize) 
            : base(x => x.Finished == false
                && x.Status == StatusEnum.Submitted.ToEnumMemberValue())
        {
            AddInclude(x => x.StatusHistory);
            AddOrder(x => x.CreatedDate);
            AddCount(batchSize);
        }
    }
}

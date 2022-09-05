using Application.Extensions;
using GovUKPayApiClient.Model;
using System.Collections.Generic;

namespace Application.UnitTests
{
    public partial class TestData
    {
        public static Entities.Refund GetSuccessfulRefund()
        {
            return new Entities.Refund()
            {
                Status = Refund.StatusEnum.Success.ToEnumMemberValue(),
                StatusHistory = new List<Entities.RefundStatusHistory>()
            };
        }

        public static Entities.Refund GetSubmittedRefund()
        {
            return new Entities.Refund()
            {
                Status = Refund.StatusEnum.Submitted.ToEnumMemberValue(),
                StatusHistory = new List<Entities.RefundStatusHistory>()
            };
        }
    }
}

using GovUKPayApiClient.Model;
using System;

namespace Application.Extensions
{
    public static class RefundExtensions
    {
        public static void Update(this Entities.Refund source, Refund result)
        {
            source.RefundId = result.RefundId;

            source.UpdateStatus(result.Status);
        }

        private static void UpdateStatus(this Entities.Refund source, Refund.StatusEnum? status)
        {
            source.Status = status.HasValue ? status.Value.ToEnumMemberValue() : null;
            source.Finished = status.IsFinished();

            source.StatusHistory.Add(new Entities.RefundStatusHistory()
            {
                CreatedDate = DateTime.Now,
                Finished = source.Finished,
                Status = status.HasValue ? status.Value.ToEnumMemberValue() : null
            });
        }

        public static void Fail(this Entities.Refund source)
        {
            source.UpdateStatus(Refund.StatusEnum.Error);
        }

        public static bool IsASuccess(this Entities.Refund source)
        {
            return source.Status == Refund.StatusEnum.Success.ToEnumMemberValue();
        }
    }
}

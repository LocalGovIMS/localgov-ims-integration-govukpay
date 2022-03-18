using Application.Entities;
using GovUKPayApiClient.Model;
using System;

namespace Application.Extensions
{
    public static class PaymentExtensions
    {
        public static void Update(this Payment source, CreatePaymentResult result)
        {
            source.NextUrl = result.Links.NextUrl.Href;
            source.PaymentId = result.PaymentId;

            source.UpdateStatus(result.State);
        }

        public static void Update(this Payment source, GetPaymentResult result)
        {
            if (!string.IsNullOrEmpty(result.SettlementSummary?.CapturedDate))
            {
                source.CapturedDate = Convert.ToDateTime(result.SettlementSummary?.CapturedDate);
            }

            source.UpdateStatus(result.State);
        }

        private static void UpdateStatus(this Payment source, PaymentState state)
        {
            source.Status = state.Status;
            source.Finished = state.Finished;

            source.StatusHistory.Add(new PaymentStatusHistory()
            {
                CreatedDate = DateTime.Now,
                Code = state.Code,
                Finished = state.Finished,
                Message = state.Message,
                Status = state.Status
            });
        }
    }
}

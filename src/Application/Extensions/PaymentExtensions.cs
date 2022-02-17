using Application.Entities;
using GovUKPayApiClient.Model;
using System;

namespace Application.Extensions
{
    public static class PaymentExtensions
    {
        public static void RecordCreatePaymentResult(this Payment source, CreatePaymentResult result)
        {
            source.NextUrl = result.Links.NextUrl.Href;
            source.PaymentId = result.PaymentId;
        }

        public static void UpdateStatus(this Payment source, PaymentState state)
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

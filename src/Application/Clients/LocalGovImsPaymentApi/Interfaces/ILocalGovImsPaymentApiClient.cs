using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Application.Clients.LocalGovImsPaymentApi
{
    public interface ILocalGovImsPaymentApiClient
    {
        Task<List<PendingTransactionModel>> GetPendingTransactions(string reference);
        Task<List<ProcessedTransactionModel>> GetProcessedTransactions(string reference);
        Task<MethodOfPaymentModel> GetCardSelfServiceMopCode();
        Task<ProcessPaymentResponseModel> ProcessPayment(string reference, ProcessPaymentModel model);
        Task<HttpStatusCode> Notify(NotificationModel model);
    }
}

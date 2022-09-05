using Application.Data;
using Application.Entities;
using Application.Extensions;
using Domain;
using Domain.Exceptions;
using GovUKPayApiClient.Api;
using LocalGovImsApiClient.Api;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class RefundRequestCommand : IRequest<RefundRequestCommandResult>
    {
        public RefundModel Refund { get; set; }
    }

    public class RefundRequestCommandHandler : IRequestHandler<RefundRequestCommand, RefundRequestCommandResult>
    {
        private readonly Func<string, ICardPaymentsApiAsync> _paymentApiClientFactory;
        private readonly Func<string, IRefundingCardPaymentsApiAsync> _refundApiClientFactory;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly IAsyncRepository<Refund> _refundRepository;
        private readonly IPendingTransactionsApiAsync _pendingTransactionsApi;
        private readonly IProcessedTransactionsApiAsync _processedTransactionsApi;
        private readonly IFundMetadataApiAsync _fundMetadataApi;
        
        private ICardPaymentsApiAsync _paymentApiClient;
        private IRefundingCardPaymentsApiAsync _refundApiClient;

        private List<PendingTransactionModel> _pendingTransactions;
        private GovUKPayApiClient.Model.GetPaymentResult _paymentResult;
        private GovUKPayApiClient.Model.Refund _refundRequestResult;
        private Payment _payment;
        private Refund _refund;
        private RefundRequestCommandResult _result;
        private ProcessPaymentModel _processPaymentModel;

        public RefundRequestCommandHandler(
            Func<string, ICardPaymentsApiAsync> paymentApiClientFactory,
            Func<string, IRefundingCardPaymentsApiAsync> refundApiClientFactory,
            IAsyncRepository<Payment> paymentRepository,
            IAsyncRepository<Refund> refundRepository,
            IPendingTransactionsApiAsync pendingTransactionsApi,
            IProcessedTransactionsApiAsync processedTransactionsApi,
            IFundMetadataApiAsync fundMetadataApi)
        {
            _paymentApiClientFactory = paymentApiClientFactory;
            _refundApiClientFactory = refundApiClientFactory;
            _paymentRepository = paymentRepository;
            _refundRepository = refundRepository;
            _pendingTransactionsApi = pendingTransactionsApi;
            _processedTransactionsApi = processedTransactionsApi;
            _fundMetadataApi = fundMetadataApi;
        }

        public async Task<RefundRequestCommandResult> Handle(RefundRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await ValidateRequest(request);

                await GetClients();

                await GetIntegrationPayment(request);

                await GetGovUKPayment();

                ValidatePaymentIsRefundable(request);

                await CreateIntegrationRefund(request);

                await SubmitGovUKPayRefundRequest();

                await UpdateRefund();

                await ProcessRefundPayment(request);

                BuildResult(request);

                return _result;
            }
            catch(RefundException ex)
            {
                return new RefundRequestCommandResult()
                {
                    Amount = null,
                    Message = ex.Message,
                    Success = false
                };
            }
            catch(Exception ex)
            {
                return new RefundRequestCommandResult()
                {
                    Amount = null,
                    Message = "Unable to process the refund",
                    Success = false
                };
            }
        }

        private async Task ValidateRequest(RefundRequestCommand request)
        {
            ValidateRequestValues(request);

            await CheckThatOriginalProcessedTransactionsExist(request);
            await CheckThatProcessedTransactionsForRefundDoNotExist(request);
            await CheckThatPendingTransactionsForRefundExist(request);
        }

        private static void ValidateRequestValues(RefundRequestCommand request)
        {
            if (string.IsNullOrEmpty(request.Refund.Reference))
            {
                throw new RefundException("The reference provided is null or empty");
            }

            if (string.IsNullOrEmpty(request.Refund.ImsReference))
            {
                throw new RefundException("The IMS reference provided is null or empty");
            }

            if (request.Refund.Amount <= 0)
            {
                throw new RefundException("The amount must be greater than zero");
            }
        }

        private async Task CheckThatOriginalProcessedTransactionsExist(RefundRequestCommand request)
        {
            try
            {
                var processedTransactions = await _processedTransactionsApi.ProcessedTransactionsSearchAsync(
                    string.Empty,
                    null,
                    string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null, 
                    null,
                    request.Refund.Reference);

                if (processedTransactions == null || !processedTransactions.Any())
                {
                    throw new RefundException("The original transaction reference provided for this refund is not for a processed payment");
                }
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404)
                    throw new RefundException("The original transaction reference provided for this refund is not for a processed payment");

                throw;
            }
        }

        private async Task CheckThatProcessedTransactionsForRefundDoNotExist(RefundRequestCommand request)
        {
            try
            {
                var processedTransactions = await _processedTransactionsApi.ProcessedTransactionsSearchAsync(
                    string.Empty,
                    null,
                    string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    request.Refund.ImsReference);

                if (processedTransactions != null && processedTransactions.Any())
                {
                    throw new RefundException("A refund for the reference provided has already been processed");
                }
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404) return; // If no processed transactions are found the API will return a 404 (Not Found) - so that's fine

                throw;
            }
        }

        private async Task CheckThatPendingTransactionsForRefundExist(RefundRequestCommand request)
        {
            try
            {
                var result = await _pendingTransactionsApi.PendingTransactionsGetAsync(request.Refund.ImsReference);

                if (result == null || !result.Any())
                {
                    throw new RefundException("The refund reference provided is no longer a valid pending refund");
                }

                _pendingTransactions = result.ToList();
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404)
                    throw new RefundException("The refund reference provided is no longer a valid pending refund");

                throw;
            }
        }

        private async Task GetClients()
        {
            var apiKeyFundMetadata = await _fundMetadataApi.FundMetadataGetAsync(_pendingTransactions.FirstOrDefault().FundCode, "GovUkPay.Api.Key");

            _paymentApiClient = _paymentApiClientFactory(apiKeyFundMetadata.Value);
            _refundApiClient = _refundApiClientFactory(apiKeyFundMetadata.Value);
        }

        private async Task GetIntegrationPayment(RefundRequestCommand request)
        {
            _payment = (await _paymentRepository.Get(x => x.PaymentId == request.Refund.Reference)).Data;
        }

        private async Task GetGovUKPayment()
        {
            _paymentResult = await _paymentApiClient.GetAPaymentAsync(_payment.PaymentId);
        }

        private void ValidatePaymentIsRefundable(RefundRequestCommand request)
        {
            if (_paymentResult.RefundSummary == null) return;
            
            if (_paymentResult.RefundSummary.Status != RefundSummaryStatus.Available)
                throw new RefundException($"The payment is not refundable. Reason: '{ RefundSummaryStatus.ToReason(_paymentResult.RefundSummary.Status) }'");

            if (_paymentResult.RefundSummary.AmountAvailable < request.Refund.Amount.ToPence())
                throw new RefundException("The amount specified is greater than the amount available to refund");
        }

        private async Task CreateIntegrationRefund(RefundRequestCommand request)
        {
            _refund = (await _refundRepository.Add(new Refund()
            {
                CreatedDate = DateTime.Now,
                Identifier = Guid.NewGuid(),
                RefundReference = request.Refund.ImsReference,
                PaymentReference = request.Refund.Reference,
                PaymentId = _payment.PaymentId,
                Amount = Convert.ToDecimal(request.Refund.Amount)
            })).Data;
        }

        private async Task SubmitGovUKPayRefundRequest()
        {
            var model = new GovUKPayApiClient.Model.PaymentRefundRequest(
                Convert.ToInt32(_refund.Amount.ToPence()), 
                Convert.ToInt32(_paymentResult.RefundSummary?.AmountAvailable ?? 0));

            _refundRequestResult = await _refundApiClient.SubmitARefundForAPaymentAsync(_payment.PaymentId, model);
        }

        private async Task UpdateRefund()
        {
            _refund.Update(_refundRequestResult);

            _refund = (await _refundRepository.Update(_refund)).Data;
        }

        // NOTE: 'Refunds do not have a submitted status if you’re using your test (‘sandbox’) account.'
        //       https://docs.payments.service.gov.uk/refunding_payments/#checking-the-status-of-a-refund
        private async Task ProcessRefundPayment(RefundRequestCommand request)
        {
            if (_refund.IsASuccess() && _refundRequestResult.Status.IsFinished())
            {
                BuildProcessPaymentModel(request);

                await _pendingTransactionsApi.PendingTransactionsProcessPaymentAsync(_refund.RefundReference, _processPaymentModel);
            }
        }

        private void BuildProcessPaymentModel(RefundRequestCommand request)
        {
            _processPaymentModel = new ProcessPaymentModel()
            {
                AuthResult = _refundRequestResult.Status.ToAuthResult(),
                PspReference = _refund.RefundId,
                MerchantReference = _refund.RefundReference,
                Fee = 0,
                CardPrefix = string.Empty,
                CardSuffix = string.Empty,
                AmountPaid = request.Refund.Amount.ToPence()
            };
        }

        private void BuildResult(RefundRequestCommand request)
        {
            _result = new RefundRequestCommandResult()
            {
                Amount = request.Refund.Amount,
                PspReference = request.Refund.ImsReference,
                Success = _refund.IsASuccess()
            };
        }
    }
}

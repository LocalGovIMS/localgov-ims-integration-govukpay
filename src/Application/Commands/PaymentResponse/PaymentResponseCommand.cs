using Application.Data;
using Application.Entities;
using Domain.Exceptions;
using GovUKPayApiClient.Model;
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
    public class PaymentResponseCommand : IRequest<ProcessPaymentResponse>
    {
        public string PaymentId { get; set; }
    }

    public class PaymentResponseCommandHandler : IRequestHandler<PaymentResponseCommand, ProcessPaymentResponse>
    {
        private readonly Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> _govUKPayApiClientFactory;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApi _pendingTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IFundMetadataApi _fundMetadataApi;

        private ProcessPaymentModel _processPaymentModel;
        private ProcessPaymentResponse _processPaymentResponse;
        private Payment _payment;
        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private GovUKPayApiClient.Api.ICardPaymentsApi _govUKPayApiClient;
        private GetPaymentResult _result;

        public PaymentResponseCommandHandler(
            Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> govUkPayApiClientFactory,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApi pendingTransactionsApi,
            LocalGovImsApiClient.Api.IFundMetadataApi fundMetadataApi)
        {
            _govUKPayApiClientFactory = govUkPayApiClientFactory;
            _paymentRepository = paymentRepository;
            _pendingTransactionsApi = pendingTransactionsApi;
            _fundMetadataApi = fundMetadataApi;
        }

        public async Task<ProcessPaymentResponse> Handle(PaymentResponseCommand request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            await GetPayment(request);

            await GetPendingTransactions();

            GetPendingTransaction();
            
            await GetClient();

            await GetPaymentStatus();

            await UpdatePayment();

            BuildProcessPaymentModel();

            await ProcessPayment();

            return _processPaymentResponse;
        }

        private void ValidateRequest(PaymentResponseCommand request)
        {
            if (string.IsNullOrEmpty(request.PaymentId))
            {
                throw new PaymentException("Unable to process the payment");
            }

            if (!Guid.TryParse(request.PaymentId, out _))
            {
                throw new PaymentException("Unable to process the payment");
            }
        }


        private async Task GetPayment(PaymentResponseCommand request)
        {
            _payment = (await _paymentRepository.Get(x => x.Identifier == Guid.Parse(request.PaymentId))).Data;
        }

        private async Task GetPendingTransactions()
        { 
            try
            {
                _pendingTransactions = (await _pendingTransactionsApi.PendingTransactionsGetAsync(_payment.Reference)).ToList();

                if (_pendingTransactions == null || !_pendingTransactions.Any())
                {
                    throw new PaymentException("The reference provided is no longer a valid pending payment");
                }
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404)
                    throw new PaymentException("The reference provided is no longer a valid pending payment");

                throw;
            }
        }

        private void GetPendingTransaction()
        {
            _pendingTransaction = _pendingTransactions.FirstOrDefault();
        }

        private async Task GetClient()
        {
            var apiKeyFundMetadata = await _fundMetadataApi.FundMetadataGetAsync(_pendingTransaction.FundCode, "GovUkPay.Api.Key");

            _govUKPayApiClient = _govUKPayApiClientFactory(apiKeyFundMetadata.Value);
        }

        private async Task GetPaymentStatus()
        {
            _result = await _govUKPayApiClient.GetAPaymentAsync(_payment.PaymentId);
        }

        private async Task UpdatePayment()
        {
            _payment.Status = _result.State.Status;
            _payment.Finished = _result.State.Finished;

            _payment = (await _paymentRepository.UpdateAsync(_payment)).Data;
        }

        private void BuildProcessPaymentModel()
        {
            // More info here: https://docs.payments.service.gov.uk/api_reference/#status-and-finished
            // TODO: Could do more with error codes (will do once we've got happy path working....)
            // TODO: extract these strings into enum style classes
            var authResultMappings = new Dictionary<string, string>()
            {
                { "created", "PENDING" },
                { "started", "PENDING" },
                { "submitted", "PENDING" },
                { "capturable", "PENDING" },
                { "success", "AUTHORISED" },
                { "failed", "REFUSED" },
                { "cancelled", "ERROR" },
                { "error", "ERROR" }
            };

            _processPaymentModel = new ProcessPaymentModel()
            {
                AuthResult = authResultMappings[_result.State.Status],
                PspReference = _payment.PaymentId,
                MerchantReference = _payment.Reference
            };        
        }

        private async Task ProcessPayment()
        {
            _processPaymentResponse = await _pendingTransactionsApi.PendingTransactionsProcessPaymentAsync(_processPaymentModel.MerchantReference, _processPaymentModel);
        }
    }
}

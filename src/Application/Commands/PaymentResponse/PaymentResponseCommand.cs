using Application.Data;
using Application.Entities;
using Application.Extensions;
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
    public class PaymentResponseCommand : IRequest<PaymentResponseCommandResult>
    {
        public string PaymentId { get; set; }
    }

    public class PaymentResponseCommandHandler : IRequestHandler<PaymentResponseCommand, PaymentResponseCommandResult>
    {
        private readonly Func<string, GovUKPayApiClient.Api.ICardPaymentsApiAsync> _govUKPayApiClientFactory;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApiAsync _pendingTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IFundMetadataApiAsync _fundMetadataApi;

        private ProcessPaymentModel _processPaymentModel;
        private ProcessPaymentResponse _processPaymentResponse;
        private Payment _payment;
        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private GovUKPayApiClient.Api.ICardPaymentsApiAsync _govUKPayApiClient;
        private GetPaymentResult _paymentResult;
        private PaymentResponseCommandResult _result;

        public PaymentResponseCommandHandler(
            Func<string, GovUKPayApiClient.Api.ICardPaymentsApiAsync> govUkPayApiClientFactory,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApiAsync pendingTransactionsApi,
            LocalGovImsApiClient.Api.IFundMetadataApiAsync fundMetadataApi)
        {
            _govUKPayApiClientFactory = govUkPayApiClientFactory;
            _paymentRepository = paymentRepository;
            _pendingTransactionsApi = pendingTransactionsApi;
            _fundMetadataApi = fundMetadataApi;
        }

        public async Task<PaymentResponseCommandResult> Handle(PaymentResponseCommand request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            await GetPayment(request);

            await GetPendingTransactions();

            GetPendingTransaction();

            await GetClient();

            await GetPaymentStatus();

            await UpdatePaymentStatus();

            BuildProcessPaymentModel();

            await ProcessPayment();

            BuildResult();

            return _result;
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
            _paymentResult = await _govUKPayApiClient.GetAPaymentAsync(_payment.PaymentId);
        }

        private async Task UpdatePaymentStatus()
        {
            _payment.Update(_paymentResult);

            _payment = (await _paymentRepository.Update(_payment)).Data;
        }

        private void BuildProcessPaymentModel()
        {
            _processPaymentModel = new ProcessPaymentModel()
            {
                AuthResult = _paymentResult.State.ToAuthResult(),
                PspReference = _payment.PaymentId,
                MerchantReference = _payment.Reference,
                Fee = Convert.ToDecimal(_paymentResult.Fee)/100,
                CardPrefix = _paymentResult.CardDetails?.FirstDigitsCardNumber,
                CardSuffix = _paymentResult.CardDetails?.LastDigitsCardNumber
            };        
        }

        private async Task ProcessPayment()
        {
            _processPaymentResponse = await _pendingTransactionsApi.PendingTransactionsProcessPaymentAsync(_processPaymentModel.MerchantReference, _processPaymentModel);
        }

        private void BuildResult()
        {
            _result = new PaymentResponseCommandResult()
            {
                NextUrl = _processPaymentResponse.RedirectUrl,
                Success = _processPaymentResponse.Success
            };
        }
    }
}

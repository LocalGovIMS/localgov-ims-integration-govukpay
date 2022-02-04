using Application.Clients.LocalGovImsPaymentApi;
using Application.Data;
using Application.Entities;
using Domain.Exceptions;
using GovUKPayApiClient.Model;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class PaymentResponseCommand : IRequest<ProcessPaymentResponseModel>
    {
        public string PaymentId { get; set; }
    }

    public class PaymentResponseCommandHandler : IRequestHandler<PaymentResponseCommand, ProcessPaymentResponseModel>
    {
        private readonly IConfiguration _configuration;
        private readonly ILocalGovImsPaymentApiClient _localGovImsPaymentApiClient;
        private readonly Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> _govUKPayApiClientFactory;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.IClient _imsClient;

        private ProcessPaymentModel _processPaymentModel;
        private ProcessPaymentResponseModel _processPaymentResponseModel;
        private Payment _payment;
        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private GovUKPayApiClient.Api.ICardPaymentsApi _govUKPayApiClient;
        private GetPaymentResult _result;

        public PaymentResponseCommandHandler(
            IConfiguration configuration,
            ILocalGovImsPaymentApiClient localGovImsPaymentApiClient,
            Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> govUKPayApiClientFactory,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.IClient imsClient)
        {
            _configuration = configuration;
            _localGovImsPaymentApiClient = localGovImsPaymentApiClient;
            _govUKPayApiClientFactory = govUKPayApiClientFactory;
            _paymentRepository = paymentRepository;
            _imsClient = imsClient;
        }

        public async Task<ProcessPaymentResponseModel> Handle(PaymentResponseCommand request, CancellationToken cancellationToken)
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

            return _processPaymentResponseModel;
        }

        private static void ValidateRequest(PaymentResponseCommand request)
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
            _pendingTransactions = await _localGovImsPaymentApiClient.GetPendingTransactions(_payment.Reference);
            if (_pendingTransactions == null || !_pendingTransactions.Any())
            {
                throw new PaymentException("The reference provided is no longer a valid pending payment");
            }
        }

        private void GetPendingTransaction()
        {
            _pendingTransaction = _pendingTransactions.FirstOrDefault();
        }

        private async Task GetClient()
        {
            // TODO: Add Swagger spec data so that we know this is a string, not an object 
            var apiKey = await _imsClient.ApiFundmetadataAsync(_pendingTransaction.FundCode, "GovUkPay.Api.Key");

            _govUKPayApiClient = _govUKPayApiClientFactory(apiKey.ToString());
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
            _processPaymentResponseModel = await _localGovImsPaymentApiClient.ProcessPayment(_processPaymentModel.MerchantReference, _processPaymentModel);
        }
    }
}

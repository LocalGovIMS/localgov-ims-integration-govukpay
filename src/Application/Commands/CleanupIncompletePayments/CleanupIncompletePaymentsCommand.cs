using Application.Data;
using Application.Entities;
using Application.Extensions;
using Domain.Exceptions;
using GovUKPayApiClient.Model;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class CleanupIncompletePaymentsCommand : IRequest<CleanupIncompletePaymentsCommandResult>
    {
    }

    public class CleanupIncompletePaymentsCommandHandler : IRequestHandler<CleanupIncompletePaymentsCommand, CleanupIncompletePaymentsCommandResult>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CleanupIncompletePaymentsCommandHandler> _logger;
        private readonly Func<string, GovUKPayApiClient.Api.ICardPaymentsApiAsync> _govUKPayApiClientFactory;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApiAsync _pendingTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IFundMetadataApiAsync _fundMetadataApi;

        private int _thresholdInMiutes = 0;
        private List<Payment> _incompletePayments;
        private Payment _incompletePayment;
        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private readonly Dictionary<string, string> _apiKeys = new();
        private GovUKPayApiClient.Api.ICardPaymentsApiAsync _govUKPayApiClient;
        private GetPaymentResult _paymentResult;
        private CleanupIncompletePaymentsCommandResult _cleanupIncompletePaymentsCommandResult;

        private int _numberOfErrors = 0;

        public CleanupIncompletePaymentsCommandHandler(
            IConfiguration configuration,
            ILogger<CleanupIncompletePaymentsCommandHandler> logger,
            Func<string, GovUKPayApiClient.Api.ICardPaymentsApiAsync> govUkPayApiClientFactory,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApiAsync pendingTransactionsApi,
            LocalGovImsApiClient.Api.IFundMetadataApiAsync fundMetadataApi)
        {
            _configuration = configuration;
            _logger = logger;
            _govUKPayApiClientFactory = govUkPayApiClientFactory;
            _paymentRepository = paymentRepository;
            _pendingTransactionsApi = pendingTransactionsApi;
            _fundMetadataApi = fundMetadataApi;
        }

        public async Task<CleanupIncompletePaymentsCommandResult> Handle(CleanupIncompletePaymentsCommand request, CancellationToken cancellationToken)
        {
            GetThresholdInMinutes();

            await GetIncompletePaymentsThatNeedClosing();

            await CleanupIncompletePayments();

            CreateResult();

            return _cleanupIncompletePaymentsCommandResult;
        }

        private void GetThresholdInMinutes()
        {
            _thresholdInMiutes = _configuration.GetValue("CleanupIncompletePaymentsCommand:Threshold", 180);
        }

        private async Task GetIncompletePaymentsThatNeedClosing()
        {
            _incompletePayments = (await _paymentRepository.List(new IncompletePayments(DateTime.Now.AddMinutes(-_thresholdInMiutes)))).Data;
                
            _logger.LogInformation(_incompletePayments.Count + " incomplete payments found");
        }

        private async Task CleanupIncompletePayments()
        {
            foreach(var incompletePayment in _incompletePayments)
            {
                _incompletePayment = incompletePayment;

                await ProcessIncompletePayment();
            }

            _logger.LogInformation(_incompletePayments.Count + " rows processed");
            _logger.LogInformation(_numberOfErrors + " failures. See logs for more details");
        }

        private async Task ProcessIncompletePayment()
        {
            try
            { 
                await GetPendingTransactions();

                GetPendingTransaction();

                await GetClient();

                await GetPaymentStatus();

                await UpdatePaymentStatus();
            }
            catch(Exception ex)
            {
                _numberOfErrors++;

                _logger.LogError(ex, "Unable to process incomplete payment record: " + _incompletePayment.Id);
            }
        }

        private async Task GetPendingTransactions()
        { 
            try
            {
                _pendingTransactions = (await _pendingTransactionsApi.PendingTransactionsGetAsync(_incompletePayment.Reference)).ToList();

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
            _govUKPayApiClient = _govUKPayApiClientFactory(await GetClientApiKey());
        }

        private async Task<string> GetClientApiKey()
        {
            string apiKeyFundMetadata;

            if (_apiKeys.ContainsKey(_pendingTransaction.FundCode))
            {
                apiKeyFundMetadata = _apiKeys[_pendingTransaction.FundCode];
            }
            else
            {
                apiKeyFundMetadata = (await _fundMetadataApi.FundMetadataGetAsync(_pendingTransaction.FundCode, "GovUkPay.Api.Key")).Value;
                _apiKeys.Add(_pendingTransaction.FundCode, apiKeyFundMetadata);
            }

            return apiKeyFundMetadata;
        }

        private async Task GetPaymentStatus()
        {
            _paymentResult = await _govUKPayApiClient.GetAPaymentAsync(_incompletePayment.PaymentId);
        }

        private async Task UpdatePaymentStatus()
        {
            _incompletePayment.Update(_paymentResult);

            _incompletePayment = (await _paymentRepository.Update(_incompletePayment)).Data;
        }

        private void CreateResult()
        {
            _cleanupIncompletePaymentsCommandResult = new CleanupIncompletePaymentsCommandResult()
            {
                TotalIdentified = _incompletePayments.Count,
                TotalClosed = _incompletePayments.Count - _numberOfErrors,
                TotalErrors = _numberOfErrors
            };
        }
    }
}

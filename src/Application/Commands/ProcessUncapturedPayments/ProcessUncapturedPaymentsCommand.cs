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
    public class ProcessUncapturedPaymentsCommand : IRequest<ProcessUncapturedPaymentsCommandResult>
    {
    }

    public class ProcessUncapturedPaymentsCommandHandler : IRequestHandler<ProcessUncapturedPaymentsCommand, ProcessUncapturedPaymentsCommandResult>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProcessUncapturedPaymentsCommandHandler> _logger;
        private readonly Func<string, GovUKPayApiClient.Api.ICardPaymentsApiAsync> _govUKPayApiClientFactory;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApiAsync _pendingTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IFundMetadataApiAsync _fundMetadataApi;

        private int _batchSize = 0;
        private List<Payment> _uncapturedPayments;
        private Payment _uncapturedPayment;
        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private readonly Dictionary<string, string> _apiKeys = new();
        private GovUKPayApiClient.Api.ICardPaymentsApiAsync _govUKPayApiClient;
        private GetPaymentResult _paymentResult;
        private ProcessUncapturedPaymentsCommandResult _processUncapturedPaymentsCommandResult;
        private ProcessFeeModel _processFeeModel;

        private int _numberOfErrors = 0;

        public ProcessUncapturedPaymentsCommandHandler(
            IConfiguration configuration,
            ILogger<ProcessUncapturedPaymentsCommandHandler> logger,
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

        public async Task<ProcessUncapturedPaymentsCommandResult> Handle(ProcessUncapturedPaymentsCommand request, CancellationToken cancellationToken)
        {
            GetBatchSize();

            await GetUncapturedPayments();

            await ProcessUncapturedPayments();

            CreateResult();

            return _processUncapturedPaymentsCommandResult;
        }

        private void GetBatchSize()
        {
            _batchSize = _configuration.GetValue("ProcessUncapturedPaymentsCommand:BatchSize", 100);
        }

        private async Task GetUncapturedPayments()
        {
            _uncapturedPayments = (await _paymentRepository.List(new UncapturedPayments(_batchSize))).Data;

            _logger.LogInformation(_uncapturedPayments.Count + " uncaptured payments found");
        }

        private async Task ProcessUncapturedPayments()
        {
            foreach(var uncapturedPayment in _uncapturedPayments)
            {
                _uncapturedPayment = uncapturedPayment;

                await ProcessUncapturedPayment();
            }

            _logger.LogInformation(_uncapturedPayments.Count + " rows processed");
            _logger.LogInformation(_numberOfErrors + " failures. See logs for more details");
        }

        private async Task ProcessUncapturedPayment()
        {
            try
            { 
                await GetPendingTransactions();

                GetPendingTransaction();

                await GetClient();

                await GetPaymentStatus();

                await UpdatePaymentStatus();

                BuildProcessFeeModel();

                await ProcessPayment();
            }
            catch(Exception ex)
            {
                _numberOfErrors++;

                _logger.LogError(ex, "Unable to process uncaptured payment record: " + _uncapturedPayment.Id);
            }
        }

        private async Task GetPendingTransactions()
        { 
            try
            {
                _pendingTransactions = (await _pendingTransactionsApi.PendingTransactionsGetAsync(_uncapturedPayment.Reference)).ToList();

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
            _paymentResult = await _govUKPayApiClient.GetAPaymentAsync(_uncapturedPayment.PaymentId);
        }

        private async Task UpdatePaymentStatus()
        {
            _uncapturedPayment.Update(_paymentResult);

            _uncapturedPayment = (await _paymentRepository.Update(_uncapturedPayment)).Data;
        }

        private void CreateResult()
        {
            _processUncapturedPaymentsCommandResult = new ProcessUncapturedPaymentsCommandResult()
            {
                TotalIdentified = _uncapturedPayments.Count,
                TotalMarkedAsCaptured = _uncapturedPayments.Count - _numberOfErrors,
                TotalErrors = _numberOfErrors
            };
        }

        private void BuildProcessFeeModel()
        {
            _processFeeModel = new ProcessFeeModel()
            {
                PspReference = _uncapturedPayment.PaymentId,
                MerchantReference = _uncapturedPayment.Reference,
                Fee = Convert.ToDecimal(_paymentResult.Fee)/100
            };
        }

        private async Task ProcessPayment()
        {
            await _pendingTransactionsApi.PendingTransactionsProcessFeeAsync(_processFeeModel.MerchantReference, _processFeeModel);
        }
    }
}

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
    public class ProcessIncompleteRefundsCommand : IRequest<ProcessIncompleteRefundsCommandResult>
    {
    }

    public class ProcessIncompleteRefundsCommandHandler : IRequestHandler<ProcessIncompleteRefundsCommand, ProcessIncompleteRefundsCommandResult>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProcessIncompleteRefundsCommandHandler> _logger;
        private readonly Func<string, GovUKPayApiClient.Api.IRefundingCardPaymentsApiAsync> _refundApiClientFactory;
        private readonly IAsyncRepository<Entities.Refund> _refundRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApiAsync _pendingTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IFundMetadataApiAsync _fundMetadataApi;

        private int _batchSize = 0;
        private List<Entities.Refund> _incompleteRefunds;
        private Entities.Refund _incompleteRefund;
        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private readonly Dictionary<string, string> _apiKeys = new();
        private GovUKPayApiClient.Api.IRefundingCardPaymentsApiAsync _refundApiClient;
        private GovUKPayApiClient.Model.Refund _refundResult;
        private ProcessPaymentModel _processPaymentModel;
        private ProcessIncompleteRefundsCommandResult _processIncompleteRefundsCommandResult;

        private int _numberOfErrors = 0;

        public ProcessIncompleteRefundsCommandHandler(
            IConfiguration configuration,
            ILogger<ProcessIncompleteRefundsCommandHandler> logger,
            Func<string, GovUKPayApiClient.Api.IRefundingCardPaymentsApiAsync> refundApiClientFactory,
            IAsyncRepository<Entities.Refund> refundRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApiAsync pendingTransactionsApi,
            LocalGovImsApiClient.Api.IFundMetadataApiAsync fundMetadataApi)
        {
            _configuration = configuration;
            _logger = logger;
            _refundApiClientFactory = refundApiClientFactory;
            _refundRepository = refundRepository;
            _pendingTransactionsApi = pendingTransactionsApi;
            _fundMetadataApi = fundMetadataApi;
        }

        public async Task<ProcessIncompleteRefundsCommandResult> Handle(ProcessIncompleteRefundsCommand request, CancellationToken cancellationToken)
        {
            GetBatchSize();

            await GetIncompleteRefunds();

            await ProcessIncompleteRefunds();

            CreateResult();

            return _processIncompleteRefundsCommandResult;
        }

        private void GetBatchSize()
        {
            _batchSize = _configuration.GetValue("ProcessIncompleteRefundsCommand:BatchSize", 100);
        }

        private async Task GetIncompleteRefunds()
        {
            _incompleteRefunds = (await _refundRepository.List(new IncompleteRefunds(_batchSize))).Data;

            _logger.LogInformation(_incompleteRefunds.Count + " incomplete refunds found");
        }

        private async Task ProcessIncompleteRefunds()
        {
            foreach(var incompleteRefund in _incompleteRefunds)
            {
                _incompleteRefund = incompleteRefund;

                await ProcessIncompleteRefund();
            }

            _logger.LogInformation(_incompleteRefunds.Count + " rows processed");
            _logger.LogInformation(_numberOfErrors + " failures. See logs for more details");
        }

        private async Task ProcessIncompleteRefund()
        {
            try
            { 
                await GetPendingTransactions();

                GetPendingTransaction();

                await GetClient();

                await GetPaymentStatus();

                await UpdateRefundStatus();

                await ProcessRefundPayment();
            }
            catch(Exception ex)
            {
                _numberOfErrors++;

                _logger.LogError(ex, "Unable to process uncaptured payment record: " + _incompleteRefund.Id);
            }
        }

        private async Task GetPendingTransactions()
        { 
            try
            {
                _pendingTransactions = (await _pendingTransactionsApi.PendingTransactionsGetAsync(_incompleteRefund.RefundReference)).ToList();

                if (_pendingTransactions == null || !_pendingTransactions.Any())
                {
                    throw new PaymentException("The reference provided is no longer a valid pending refund");
                }
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404)
                    throw new PaymentException("The reference provided is no longer a valid pending refund");

                throw;
            }
        }

        private void GetPendingTransaction()
        {
            _pendingTransaction = _pendingTransactions.FirstOrDefault();
        }

        private async Task GetClient()
        {    
            _refundApiClient = _refundApiClientFactory(await GetClientApiKey());
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
            _refundResult = await _refundApiClient.GetAPaymentRefundAsync(_incompleteRefund.PaymentId, _incompleteRefund.RefundId);
        }

        private async Task UpdateRefundStatus()
        {
            _incompleteRefund.Update(_refundResult);

            _incompleteRefund = (await _refundRepository.Update(_incompleteRefund)).Data;
        }

        private async Task ProcessRefundPayment()
        {
            if (_incompleteRefund.IsASuccess() && _refundResult.Status.IsFinished())
            {
                BuildProcessPaymentModel();

                await _pendingTransactionsApi.PendingTransactionsProcessPaymentAsync(_incompleteRefund.RefundReference, _processPaymentModel);
            }
        }

        private void BuildProcessPaymentModel()
        {
            _processPaymentModel = new ProcessPaymentModel()
            {
                AuthResult = _refundResult.Status.ToAuthResult(),
                PspReference = _incompleteRefund.RefundId,
                MerchantReference = _incompleteRefund.RefundReference,
                Fee = 0,
                CardPrefix = string.Empty,
                CardSuffix = string.Empty,
                AmountPaid = _incompleteRefund.Amount.ToPence()
            };
        }

        private void CreateResult()
        {
            _processIncompleteRefundsCommandResult = new ProcessIncompleteRefundsCommandResult()
            {
                TotalIdentified = _incompleteRefunds.Count,
                TotalProcessed = _incompleteRefunds.Count - _numberOfErrors,
                TotalErrors = _numberOfErrors
            };
        }
    }
}

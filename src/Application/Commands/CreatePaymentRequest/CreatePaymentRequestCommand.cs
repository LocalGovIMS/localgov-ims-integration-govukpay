using Application.Cryptography;
using Application.Data;
using Application.Entities;
using Application.Extensions;
using Domain.Exceptions;
using GovUKPayApiClient.Model;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;
using MediatR;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class CreatePaymentRequestCommand : IRequest<CreatePaymentRequestCommandResult>
    {
        public string Reference { get; set; }

        public string Hash { get; set; }
    }

    public class CreatePaymentRequestCommandHandler : IRequestHandler<CreatePaymentRequestCommand, CreatePaymentRequestCommandResult>
    {
        private readonly ICryptographyService _cryptographyService;
        private readonly Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> _govUKPayApiClientFactory;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApiAsync _pendingTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IProcessedTransactionsApi _processedTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IFundMetadataApi _fundMetadataApi;
        private readonly LocalGovImsApiClient.Api.IFundsApi _fundsApi;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private GovUKPayApiClient.Api.ICardPaymentsApi _govUkPayApiClient;

        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private Payment _payment;
        private CreatePaymentResult _createPaymentResult;
        private CreatePaymentRequestCommandResult _result;

        public CreatePaymentRequestCommandHandler(
            ICryptographyService cryptographyService,
            Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> govUKPayApiClientFactory,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApi pendingTransactionsApi,
            LocalGovImsApiClient.Api.IProcessedTransactionsApi processedTransactionsApi,
            LocalGovImsApiClient.Api.IFundMetadataApi fundMetadataApi,
            LocalGovImsApiClient.Api.IFundsApi fundsApi,
            IHttpContextAccessor httpContextAccessor)
        {
            _cryptographyService = cryptographyService;
            _govUKPayApiClientFactory = govUKPayApiClientFactory;
            _paymentRepository = paymentRepository;
            _pendingTransactionsApi = pendingTransactionsApi;
            _processedTransactionsApi = processedTransactionsApi;
            _fundMetadataApi = fundMetadataApi;
            _fundsApi = fundsApi;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CreatePaymentRequestCommandResult> Handle(CreatePaymentRequestCommand request, CancellationToken cancellationToken)
        {
            await ValidateRequest(request);

            GetPendingTransaction();

            await GetClient();

            await CreatePayment(request);

            await CreateGovUkPayPayment(request);

            await UpdatePayment();

            BuildResult();

            return _result;
        }

        private async Task ValidateRequest(CreatePaymentRequestCommand request)
        {
            ValidateRequestValues(request);
            await CheckThatProcessedTransactionsDoNotExist(request);
            await CheckThatAPendingTransactionExists(request);
        }

        private void ValidateRequestValues(CreatePaymentRequestCommand request)
        {
            if (string.IsNullOrEmpty(request.Reference))
            {
                throw new PaymentException("The reference provided is null or empty");
            }

            if(string.IsNullOrEmpty(request.Hash))
            {
                throw new PaymentException("The hash provided is null or empty");
            }

            if(request.Hash != _cryptographyService.GetHash(request.Reference))
            {
                throw new PaymentException("The hash is invalid");
            }
        }

        private async Task CheckThatProcessedTransactionsDoNotExist(CreatePaymentRequestCommand request)
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
                    request.Reference,
                    string.Empty);

                if (processedTransactions != null && processedTransactions.Any())
                {
                    throw new PaymentException("The reference provided is no longer a valid pending payment");
                }
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404) return; // If no processed transactions are found the API will return a 404 (Not Found) - so that's fine

                throw;
            }
        }

        private async Task CheckThatAPendingTransactionExists(CreatePaymentRequestCommand request)
        {
            try
            {
                var result = await _pendingTransactionsApi.PendingTransactionsGetAsync(request.Reference);

                if (result == null || !result.Any())
                {
                    throw new PaymentException("The reference provided is no longer a valid pending payment");
                }

                _pendingTransactions = result.ToList();
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

            _govUkPayApiClient = _govUKPayApiClientFactory(apiKeyFundMetadata.Value);
        }

        private async Task CreatePayment(CreatePaymentRequestCommand request)
        {
            _payment = (await _paymentRepository.Add(new Payment()
            {
                Amount = Convert.ToDecimal(_pendingTransactions.Sum(x => x.Amount)),
                CreatedDate = DateTime.Now,
                Identifier = Guid.NewGuid(),
                Reference = request.Reference
            })).Data;
        }

        private async Task CreateGovUkPayPayment(CreatePaymentRequestCommand request)
        {
            try
            {
                var model = new CreateCardPaymentRequest(
                    Convert.ToDecimal(_pendingTransactions.Sum(x => x.Amount)).ToPence(),
                    false,
                    await GetDescription(),
                    null,
                    null,
                    null,
                    false,
                    new PrefilledCardholderDetails()
                    {
                        CardholderName = _pendingTransaction.PayeeName,
                        BillingAddress = new Address()
                        {
                            Line1 = _pendingTransaction.PayeePremiseNumber,
                            Line2 = _pendingTransaction.PayeeStreet,
                            City = _pendingTransaction.PayeeTown,
                            Postcode = _pendingTransaction.PayeePostCode
                        }
                    },
                    request.Reference,
                    GetReturnUrl());

                _createPaymentResult = await _govUkPayApiClient.CreateAPaymentAsync(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to create payment");
            }
        }

        private async Task<string> GetDescription()
        {
            var fund = await _fundsApi.FundsGetAsync(_pendingTransaction.FundCode);

            return fund.FundName;
        }

        private string GetReturnUrl()
        {
            var host = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_httpContextAccessor.HttpContext.Request.PathBase}";

            return $"{host}/Payment/PaymentResponse/" + _payment.Identifier;
        }

        private async Task UpdatePayment()
        {
            _payment.Update(_createPaymentResult);

            _payment = (await _paymentRepository.Update(_payment)).Data;
        }

        private void BuildResult()
        {
            _result = new CreatePaymentRequestCommandResult()
            {
                NextUrl = _payment.NextUrl,
                PaymentId = _payment.PaymentId,
                Status = _payment.Status,
                Finished = _payment.Finished
            };
        }
    }
}

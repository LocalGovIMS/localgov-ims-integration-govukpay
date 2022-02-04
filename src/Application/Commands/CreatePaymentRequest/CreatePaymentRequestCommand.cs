using Application.Clients.LocalGovImsPaymentApi;
using Application.Cryptography;
using Application.Data;
using Application.Entities;
using Application.Extensions;
using Domain.Exceptions;
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
        private readonly ILocalGovImsPaymentApiClient _localGovImsPaymentApiClient;
        private readonly Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> _govUKPayApiClientFactory;

        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.IClient _localGovImsApiClient;

        private readonly IHttpContextAccessor _httpContextAccessor;

        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private GovUKPayApiClient.Api.ICardPaymentsApi _govUkPayApiClient;
        private Payment _payment;
        private CreatePaymentRequestCommandResult _result;

        public CreatePaymentRequestCommandHandler(
            ICryptographyService cryptographyService,
            ILocalGovImsPaymentApiClient localGovImsPaymentApiClient,
            Func<string, GovUKPayApiClient.Api.ICardPaymentsApi> govUKPayApiClientFactory,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.IClient localGovImsApiClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _cryptographyService = cryptographyService;
            _localGovImsPaymentApiClient = localGovImsPaymentApiClient;
            _govUKPayApiClientFactory = govUKPayApiClientFactory; 
            _paymentRepository = paymentRepository;
            _localGovImsApiClient = localGovImsApiClient;
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

            return _result;
        }

        private async Task ValidateRequest(CreatePaymentRequestCommand request)
        {
            if (request.Reference == null || request.Hash == null || request.Hash != _cryptographyService.GetHash(request.Reference))
            {
                throw new PaymentException("The reference provided is not valid");
            }
            
            var processedTransactions = await _localGovImsPaymentApiClient.GetProcessedTransactions(request.Reference);
            if (processedTransactions != null && processedTransactions.Any())
            {
                throw new PaymentException("The reference provided is no longer a valid pending payment");
            }

            _pendingTransactions = await _localGovImsPaymentApiClient.GetPendingTransactions(request.Reference);
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
            var apiKey = await _localGovImsApiClient.ApiFundmetadataAsync(_pendingTransaction.FundCode, "GovUkPay.Api.Key");

            _govUkPayApiClient = _govUKPayApiClientFactory(apiKey);
        }

        private async Task CreatePayment(CreatePaymentRequestCommand request)
        {
            _payment = (await _paymentRepository.AddAsync(new Payment()
            {
                Amount = _pendingTransactions.Sum(x => x.Amount ?? 0),
                CreatedDate = DateTime.Now,
                Identifier = Guid.NewGuid(),
                Reference = request.Reference
            })).Data;
        }

        private async Task CreateGovUkPayPayment(CreatePaymentRequestCommand request)
        {
            try
            {
                var model = new GovUKPayApiClient.Model.CreateCardPaymentRequest(
                    _pendingTransactions.Sum(x => x.Amount ?? 0).ToPence(),
                    false,
                    await GetDescription(),
                    null,
                    null,
                    null,
                    false,
                    new GovUKPayApiClient.Model.PrefilledCardholderDetails()
                    {
                        CardholderName = _pendingTransaction.PayeeName,
                        BillingAddress = new GovUKPayApiClient.Model.Address()
                        {
                            Line1 = _pendingTransaction.PayeePremiseNumber,
                            Line2 = _pendingTransaction.PayeeStreet,
                            City = _pendingTransaction.PayeeTown,
                            Postcode = _pendingTransaction.PayeePostCode
                        }
                    },
                    request.Reference,
                    GetReturnUrl());

                var result = await _govUkPayApiClient.CreateAPaymentAsync(model);

                _result = new CreatePaymentRequestCommandResult()
                {
                    NextUrl = result.Links.NextUrl.Href,
                    PaymentId = result.PaymentId,
                    Status = result.State.Status,
                    Finished = Convert.ToBoolean(result.State.Finished)
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to create payment");
            }
        }

        private async Task<string> GetDescription()
        {
            var fund = await _localGovImsApiClient.ApiFundsAsync(_pendingTransaction.FundCode);

            return fund.FundName;
        }

        private string GetReturnUrl()
        {
            var host = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_httpContextAccessor.HttpContext.Request.PathBase}";

            return $"{host}/Payment/PaymentResponse/" + _payment.Identifier;
        }

        private async Task UpdatePayment()
        {
            _payment.NextUrl = _result.NextUrl;
            _payment.PaymentId = _result.PaymentId;
            _payment.Status = _result.Status;
            _payment.Finished = _result.Finished;

            _payment = (await _paymentRepository.UpdateAsync(_payment)).Data;
        }
    }
}

using Application.Commands;
using Application.Cryptography;
using Application.Data;
using Application.Entities;
using Application.Result;
using Domain;
using Domain.Exceptions;
using FluentAssertions;
using GovUKPayApiClient.Api;
using GovUKPayApiClient.Model;
using LocalGovImsApiClient.Api;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Command = Application.Commands.RefundRequestCommand;
using Handler = Application.Commands.RefundRequestCommandHandler;

namespace Application.UnitTests.Commands.RefundRequest
{
    public class HandleTests
    {
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<Func<string, ICardPaymentsApiAsync>> _mockPaymentApiClientFactory = new();
        private readonly Mock<Func<string, IRefundingCardPaymentsApiAsync>> _mockRefundApiClientFactory = new();
        private readonly Mock<IAsyncRepository<Payment>> _mockPaymentRepository = new();
        private readonly Mock<IAsyncRepository<Entities.Refund>> _mockRefundRepository = new();
        private readonly Mock<IPendingTransactionsApiAsync> _mockPendingTransactionsApi = new();
        private readonly Mock<IProcessedTransactionsApiAsync> _mockProcessedTransactionsApi = new();
        private readonly Mock<IFundMetadataApiAsync> _mockFundMetadataApi = new();

        private readonly Mock<ICardPaymentsApiAsync> _mockGovUKPayPaymentApiClient = new();
        private readonly Mock<IRefundingCardPaymentsApiAsync> _mockGovUKPayRefundApiClient = new();

        private RefundModel _refundModel;

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockPaymentApiClientFactory.Object,
                _mockRefundApiClientFactory.Object,
                _mockPaymentRepository.Object,
                _mockRefundRepository.Object,
                _mockPendingTransactionsApi.Object,
                _mockProcessedTransactionsApi.Object,
                _mockFundMetadataApi.Object);

            _refundModel = GetRefundModel();

            SetupClients(_refundModel, RefundSummaryStatus.Available, GovUKPayApiClient.Model.Refund.StatusEnum.Success);
            SetupCommand(_refundModel);
        }

        private void SetupClients(RefundModel refundModel, string paymentRefundSummaryStatus, GovUKPayApiClient.Model.Refund.StatusEnum submitRefundStatus)
        {
            _mockProcessedTransactionsApi.Setup(x => x.ProcessedTransactionsSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    refundModel.Reference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProcessedTransactionModel>() {
                    new ProcessedTransactionModel()
                    {
                        Reference = refundModel.Reference
                    }
                });

            _mockProcessedTransactionsApi.Setup(x => x.ProcessedTransactionsSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    refundModel.ImsReference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<ProcessedTransactionModel>)null);

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(
                    refundModel.ImsReference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PendingTransactionModel>() {
                    new PendingTransactionModel()
                    {
                        Reference = refundModel.ImsReference
                    }
                });

            _mockFundMetadataApi.Setup(x => x.FundMetadataGetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FundMetadataModel()
                {
                    FundCode = "F1",
                    Key = "Key",
                    Value = "Value"
                });

            _mockPaymentApiClientFactory.Setup(x => x.Invoke(It.IsAny<string>()))
                .Returns(_mockGovUKPayPaymentApiClient.Object);

            _mockRefundApiClientFactory.Setup(x => x.Invoke(It.IsAny<string>()))
                .Returns(_mockGovUKPayRefundApiClient.Object);

            _mockPaymentRepository.Setup(x => x.Get(It.IsAny<Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(new OperationResult<Payment>(true) {  
                    Data = new Payment()
                    {
                        Reference = refundModel.Reference,
                        PaymentId = "12345"
                    }
                });

            var paymentState = Newtonsoft.Json.JsonConvert.DeserializeObject<PaymentState>($"{{ \"code\":\"test\", \"finished\": true, \"message\":\"method\", \"status\":\"success\" }}");
            var refundSummary = Newtonsoft.Json.JsonConvert.DeserializeObject<RefundSummary>($"{{ \"status\":\"{paymentRefundSummaryStatus}\", \"amount_available\": 1000 }}");

            _mockGovUKPayPaymentApiClient.Setup(x => x.GetAPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetPaymentResult(
                    null,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    null,
                    refundSummary,
                    null,
                    paymentState
                    ));

            _mockRefundRepository.Setup(x => x.Add(It.IsAny<Entities.Refund>()))
                .ReturnsAsync((Entities.Refund x) => new OperationResult<Entities.Refund>(true) { Data = x });

            var refundResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<GovUKPayApiClient.Model.Refund>($"{{ \"status\":\"{submitRefundStatus}\", \"refund_id\": \"12345\" }}");

            _mockGovUKPayRefundApiClient.Setup(x => x.SubmitARefundForAPaymentAsync(
                    It.IsAny<string>(), 
                    It.IsAny<PaymentRefundRequest>(), 
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(refundResponse);

            _mockRefundRepository.Setup(x => x.Update(It.IsAny<Entities.Refund>()))
                .ReturnsAsync((Entities.Refund x) => new OperationResult<Entities.Refund>(true) { Data = x });

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsProcessPaymentAsync(
                It.IsAny<string>(),
                It.IsAny<ProcessPaymentModel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()));
        }

        private void SetupCommand(RefundModel refund)
        {
            _command = new Command() { Refund = refund };
        }

        private RefundModel GetRefundModel()
        {
            return new RefundModel()
            {
                Amount = 10,
                ImsReference = "ABBCDEFG",
                Reference = "HIJKLMNO",
                TransactionDate = DateTime.Now
            };
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_the_reference_is_null()
        {
            // Arrange
            var refundModel = GetRefundModel();
            refundModel.Reference = null;
            SetupCommand(refundModel);

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The reference provided is null or empty");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_the_reference_is_empty()
        {
            // Arrange
            var refundModel = GetRefundModel();
            refundModel.Reference = string.Empty;
            SetupCommand(refundModel);

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The reference provided is null or empty");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_the_IMS_reference_is_null()
        {
            // Arrange
            var refundModel = GetRefundModel();
            refundModel.ImsReference = null;
            SetupCommand(refundModel);

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The IMS reference provided is null or empty");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_the_IMS_reference_is_empty()
        {
            // Arrange
            var refundModel = GetRefundModel();
            refundModel.ImsReference = string.Empty;
            SetupCommand(refundModel);

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The IMS reference provided is null or empty");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Handle_returns_expected_result_when_the_amount_is_less_than_or_eqaul_to_zero(int amount)
        {
            // Arrange
            var refundModel = GetRefundModel();
            refundModel.Amount = amount;
            SetupCommand(refundModel);

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The amount must be greater than zero");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_a_processed_payment_cannot_be_found_for_the_original_transcation_reference()
        {
            // Arrange
            _mockProcessedTransactionsApi.Setup(x => x.ProcessedTransactionsSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    _refundModel.Reference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<ProcessedTransactionModel>)null);

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The original transaction reference provided for this refund is not for a processed payment");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_a_processed_payment_lookup_for_the_original_transcation_reference_throws_an_ApiException_with_a_404_error_code()
        {
            // Arrange
            _mockProcessedTransactionsApi.Setup(x => x.ProcessedTransactionsSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    _refundModel.Reference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException() { ErrorCode = 404 });

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The original transaction reference provided for this refund is not for a processed payment");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_a_processed_payment_cannot_be_found_for_the_refund_transcation_reference()
        {
            // Arrange
            _mockProcessedTransactionsApi.Setup(x => x.ProcessedTransactionsSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    _refundModel.ImsReference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProcessedTransactionModel>
                {
                    new ProcessedTransactionModel()
                });

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("A refund for the reference provided has already been processed");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_a_processed_payment_lookup_for_the_refund_transaction_reference_throws_an_ApiException_with_a_none_404_error_code()
        {
            // Arrange
            _mockProcessedTransactionsApi.Setup(x => x.ProcessedTransactionsSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    _refundModel.ImsReference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException() { ErrorCode = 401 });

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Unable to process the refund");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_a_pending_payment_is_empty_for_the_refund_transcation_reference()
        {
            // Arrange
            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(
                    _refundModel.ImsReference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<PendingTransactionModel>)null);

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The refund reference provided is no longer a valid pending refund");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_a_pending_payment_is_null_for_the_refund_transcation_reference()
        {
            // Arrange
            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(
                    _refundModel.ImsReference,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PendingTransactionModel>());

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The refund reference provided is no longer a valid pending refund");
        }

        [Fact]
        public async Task Handle_returns_expected_result_when_a_pending_payment_lookup_for_the_refund_transaction_reference_throws_an_ApiException_with_a_404_error_code()
        {
            // Arrange
            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(
                     _refundModel.ImsReference,
                     It.IsAny<int>(),
                     It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new ApiException() { ErrorCode = 404 });

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The refund reference provided is no longer a valid pending refund");
        }

        [Theory]
        [InlineData(RefundSummaryStatus.Full)]
        [InlineData(RefundSummaryStatus.Unavailable)]
        public async Task Handle_returns_expected_result_when_the_refund_is_not_available(string status)
        {
            // Arrange
            var paymentState = Newtonsoft.Json.JsonConvert.DeserializeObject<PaymentState>($"{{ \"code\":\"test\", \"finished\": true, \"message\":\"method\", \"status\":\"success\" }}");
            var refundSummary = Newtonsoft.Json.JsonConvert.DeserializeObject<RefundSummary>($"{{ \"status\":\"{status}\", \"amount_available\": 1000 }}");

            _mockGovUKPayPaymentApiClient.Setup(x => x.GetAPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetPaymentResult(
                    null,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    null,
                    refundSummary,
                    null,
                    paymentState
                    ));

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be($"The payment is not refundable. Reason: '{RefundSummaryStatus.ToReason(status)}'");
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(2000)]
        public async Task Handle_returns_expected_result_when_the_refund_amount_is_more_than_the_amount_available_to_refud(int amountAvaiable)
        {
            // Arrange
            var refund = GetRefundModel();
            refund.Amount = amountAvaiable + 1;
            SetupCommand(refund);

            var paymentState = Newtonsoft.Json.JsonConvert.DeserializeObject<PaymentState>($"{{ \"code\":\"test\", \"finished\": true, \"message\":\"method\", \"status\":\"success\" }}");
            var refundSummary = Newtonsoft.Json.JsonConvert.DeserializeObject<RefundSummary>($"{{ \"status\":\"{RefundSummaryStatus.Available}\", \"amount_available\": {amountAvaiable} }}");

            _mockGovUKPayPaymentApiClient.Setup(x => x.GetAPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetPaymentResult(
                    null,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    null,
                    refundSummary,
                    null,
                    paymentState
                    ));

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("The amount specified is greater than the amount available to refund");
        }

        [Fact]
        public async Task Handle_returns_RefundRequestCommandResult_when_successful()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Should().BeOfType<RefundRequestCommandResult>();
        }
    }
}

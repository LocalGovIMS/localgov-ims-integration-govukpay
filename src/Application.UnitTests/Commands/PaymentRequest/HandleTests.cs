using Application.Commands;
using Application.Cryptography;
using Application.Data;
using Application.Entities;
using Application.Result;
using Domain.Exceptions;
using FluentAssertions;
using GovUKPayApiClient.Model;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Command = Application.Commands.CreatePaymentRequestCommand;
using Handler = Application.Commands.CreatePaymentRequestCommandHandler;

namespace Application.UnitTests.Commands.PaymentRequest
{
    public class HandleTests
    {
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<ICryptographyService> _mockCryptographyService = new Mock<ICryptographyService>();
        private readonly Mock<Func<string, GovUKPayApiClient.Api.ICardPaymentsApi>> _mockGovUKPayApiClientFactory = new Mock<Func<string, GovUKPayApiClient.Api.ICardPaymentsApi>>();
        private readonly Mock<IAsyncRepository<Payment>> _mockPaymentRepository = new Mock<IAsyncRepository<Payment>>();
        private readonly Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi> _mockPendingTransactionsApi = new Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi>();
        private readonly Mock<LocalGovImsApiClient.Api.IProcessedTransactionsApi> _mockProcessedTransactionsApi = new Mock<LocalGovImsApiClient.Api.IProcessedTransactionsApi>();
        private readonly Mock<LocalGovImsApiClient.Api.IFundMetadataApi> _mockFundMetadataApi = new Mock<LocalGovImsApiClient.Api.IFundMetadataApi>();
        private readonly Mock<LocalGovImsApiClient.Api.IFundsApi> _mockFundsApi = new Mock<LocalGovImsApiClient.Api.IFundsApi>();
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        private readonly Mock<GovUKPayApiClient.Api.ICardPaymentsApi> _mockGovUKPayApiClient = new Mock<GovUKPayApiClient.Api.ICardPaymentsApi>();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockCryptographyService.Object,
                _mockGovUKPayApiClientFactory.Object,
                _mockPaymentRepository.Object,
                _mockPendingTransactionsApi.Object,
                _mockProcessedTransactionsApi.Object,
                _mockFundMetadataApi.Object,
                _mockFundsApi.Object,
                _mockHttpContextAccessor.Object);

            SetupClient();
            SetupCryptographyService();
            SetupCommand("reference", "hash");
        }

        private void SetupClient()
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
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<ProcessedTransactionModel>)null);

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PendingTransactionModel>() {
                    new PendingTransactionModel()
                    {
                        Reference = "Test"
                    }
                });

            _mockFundMetadataApi.Setup(x => x.FundMetadataGetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FundMetadataModel()
                {
                    FundCode = "F1",
                    Key = "Key",
                    Value = "Value"
                });

            _mockFundsApi.Setup(x => x.FundsGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FundModel()
                {
                    FundCode = "F1",
                    FundName = "Fund Name"
                });

            var nextLink = Newtonsoft.Json.JsonConvert.DeserializeObject<Link>("{ \"href\":\"test\", \"method\":\"method\" }");
            var paymentState = Newtonsoft.Json.JsonConvert.DeserializeObject<PaymentState>("{ \"code\":\"test\", \"finished\": true, \"message\":\"method\", \"status\":\"success\" }");

            _mockGovUKPayApiClient.Setup(x => x.CreateAPaymentAsync(It.IsAny<CreateCardPaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreatePaymentResult(
                    new PaymentLinks(null, null, null, nextLink, null, null, null),
                    10,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    "paymentId",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    paymentState
                ));

            _mockGovUKPayApiClientFactory.Setup(x => x.Invoke(It.IsAny<string>()))
                .Returns(_mockGovUKPayApiClient.Object);

            _mockPaymentRepository.Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync(new OperationResult<Payment>(true) { Data = new Payment() { Identifier = Guid.NewGuid() } });

            _mockPaymentRepository.Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
                .ReturnsAsync(new OperationResult<Payment>(true) { Data = new Payment() { Identifier = Guid.NewGuid() } });

            _mockHttpContextAccessor.SetupGet(x => x.HttpContext.Request.Scheme).Returns("https");
            _mockHttpContextAccessor.SetupGet(x => x.HttpContext.Request.Host).Returns(new HostString("www.test.com"));
            _mockHttpContextAccessor.SetupGet(x => x.HttpContext.Request.PathBase).Returns("/test");
        }

        private void SetupCryptographyService()
        {
            _mockCryptographyService.Setup(x => x.GetHash(It.IsAny<string>()))
                .Returns("hash");
        }

        private void SetupCommand(string reference, string hash)
        {
            _command = new Command() { Reference = reference, Hash = hash };
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_the_reference_is_null()
        {
            // Arrange
            SetupCommand(null, "hash");

            // Act
            async Task task() => await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is not valid");
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_the_hash_is_null()
        {
            // Arrange
            SetupCommand("reference", null);

            // Act
            async Task task() => await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is not valid");
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_the_hash_does_not_match_the_computed_hash()
        {
            // Arrange
            SetupCommand("reference", "hash that doesn't match");

            // Act
            async Task task() => await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is not valid");
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_processed_transactions_exists_for_the_reference()
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
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProcessedTransactionModel>() {
                    new ProcessedTransactionModel()
                    {
                        Reference = "Test"
                    }
                });

            // Act
            async Task task() => await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is no longer a valid pending payment");
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_pending_transactions_do_not_exist_for_the_reference()
        {
            // Arrange
            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(404, string.Empty, null, null));

            // Act
            async Task task() => await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is no longer a valid pending payment");
        }

        [Fact]
        public async Task Handle_returns_CreatePaymentRequestCommandResult_when_successful()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Should().BeOfType<CreatePaymentRequestCommandResult>();
        }

        [Fact]
        public async Task Handle_returns_expected_values_when_successful()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Finished.Should().BeTrue();
            result.NextUrl.Should().Be("test");
            result.PaymentId.Should().Be("paymentId");
            result.Status.Should().Be("success");
        }
    }
}

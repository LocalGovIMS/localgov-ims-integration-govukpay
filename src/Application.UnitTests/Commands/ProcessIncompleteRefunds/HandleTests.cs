using Application.Commands;
using Application.Data;
using Application.Result;
using FluentAssertions;
using GovUKPayApiClient.Model;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Command = Application.Commands.ProcessIncompleteRefundsCommand;
using Handler = Application.Commands.ProcessIncompleteRefundsCommandHandler;

namespace Application.UnitTests.Commands.ProcessIncompleteRefunds
{
    public class HandleTests
    {
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<IConfiguration> _mockConfiguration = new();
        private readonly Mock<ILogger<Handler>> _mockLogger = new();
        private readonly Mock<Func<string, GovUKPayApiClient.Api.IRefundingCardPaymentsApiAsync>> _mockRefundApiClientFactory = new();
        private readonly Mock<IAsyncRepository<Entities.Refund>> _mockRefundRepository = new();
        private readonly Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi> _mockPendingTransactionsApi = new();
        private readonly Mock<LocalGovImsApiClient.Api.IFundMetadataApi> _mockFundMetadataApi = new();

        private readonly Mock<GovUKPayApiClient.Api.IRefundingCardPaymentsApiAsync> _mockRefundApiClient = new();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockRefundApiClientFactory.Object,
                _mockRefundRepository.Object,
                _mockPendingTransactionsApi.Object,
                _mockFundMetadataApi.Object);

            SetupClient();
            SetupCommand();
        }

        private void SetupClient()
        {
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(a => a.Value).Returns("100");

            _mockConfiguration.Setup(x => x.GetSection("ProcessIncompleteRefundsCommand:BatchSize")).Returns(configSection.Object);

            _mockRefundRepository.Setup(x => x.List(It.IsAny<ISpecification<Entities.Refund>>(), It.IsAny<bool>()))
                .ReturnsAsync(new OperationResult<List<Entities.Refund>>(true)
                {
                    Data = new List<Entities.Refund>()
                    {
                        new Entities.Refund() { Identifier = Guid.NewGuid(), RefundReference = "Test1", PaymentId = "PaymentId1", RefundId = "RefundId1" },
                        new Entities.Refund() { Identifier = Guid.NewGuid(), RefundReference = "Test2", PaymentId = "PaymentId2", RefundId = "RefundId2" }
                    }
                });

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PendingTransactionModel>() {
                    new PendingTransactionModel()
                    {
                        Reference = "Test",
                        FundCode = "F1"
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

            _mockRefundApiClientFactory.Setup(x => x.Invoke(It.IsAny<string>()))
                .Returns(_mockRefundApiClient.Object);

            var settlementSummary = Newtonsoft.Json.JsonConvert.DeserializeObject<RefundSettlementSummary>("{ \"code\":\"test\", \"finished\": true, \"message\":\"method\", \"status\":\"success\" }");

            _mockRefundApiClient.Setup(x => x.GetAPaymentRefundAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GovUKPayApiClient.Model.Refund(
                    null,
                    settlementSummary));

            _mockRefundRepository.Setup(x => x.Update(It.IsAny<Entities.Refund>()))
                .ReturnsAsync(new OperationResult<Entities.Refund>(true) { Data = new Entities.Refund() { Identifier = Guid.NewGuid(), PaymentId = "paymentId", RefundReference = "refernce" } });

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsProcessPaymentAsync(
                It.IsAny<string>(), 
                It.IsAny<ProcessPaymentModel>(),
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()));
        }

        private void SetupCommand()
        {
            _command = new Command();
        }

        [Fact]
        public async Task Handle_returns_expected_error_count_when_pending_transactions_do_not_exist_for_the_reference()
        {
            // Arrange
            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsGetAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(404, string.Empty, null, null));

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Should().BeOfType<ProcessIncompleteRefundsCommandResult>();
            result.TotalIdentified.Should().Be(2);
            result.TotalProcessed.Should().Be(0);
            result.TotalErrors.Should().Be(2);
        }

        [Fact]
        public async Task Handle_returns_ProcessIncompleteRefundsCommandResult_when_successful()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Should().BeOfType<ProcessIncompleteRefundsCommandResult>();
            result.TotalIdentified.Should().Be(2);
            result.TotalProcessed.Should().Be(2);
            result.TotalErrors.Should().Be(0);
        }

        [Fact]
        public async Task Handle_should_only_retrieve_fundmetadata_once()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            _mockFundMetadataApi.Verify(x => x.FundMetadataGetAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(),
                    It.IsAny<int>(), 
                    It.IsAny<CancellationToken>())
                , Times.Once);
        }
    }
}

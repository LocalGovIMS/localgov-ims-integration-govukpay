using Application.Commands;
using Application.Data;
using Application.Entities;
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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Command = Application.Commands.ProcessUncapturedPaymentsCommand;
using Handler = Application.Commands.ProcessUncapturedPaymentsCommandHandler;

namespace Application.UnitTests.Commands.ProcessUncapturedPayments
{
    public class HandleTests
    {
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<IConfiguration> _mockConfiguration = new();
        private readonly Mock<ILogger<Handler>> _mockLogger = new();
        private readonly Mock<Func<string, GovUKPayApiClient.Api.ICardPaymentsApi>> _mockGovUKPayApiClientFactory = new();
        private readonly Mock<IAsyncRepository<Payment>> _mockPaymentRepository = new();
        private readonly Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi> _mockPendingTransactionsApi = new();
        private readonly Mock<LocalGovImsApiClient.Api.IFundMetadataApi> _mockFundMetadataApi = new();

        private readonly Mock<GovUKPayApiClient.Api.ICardPaymentsApi> _mockGovUKPayApiClient = new();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockGovUKPayApiClientFactory.Object,
                _mockPaymentRepository.Object,
                _mockPendingTransactionsApi.Object,
                _mockFundMetadataApi.Object);

            SetupClient();
            SetupCommand();
        }

        private void SetupClient()
        {
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(a => a.Value).Returns("100");

            _mockConfiguration.Setup(x => x.GetSection("ProcessUncapturedPaymentsCommand:BatchSize")).Returns(configSection.Object);

            _mockPaymentRepository.Setup(x => x.List(It.IsAny<ISpecification<Payment>>(), It.IsAny<bool>()))
                .ReturnsAsync(new OperationResult<List<Payment>>(true)
                {
                    Data = new List<Payment>()
                    {
                        new Payment() { Identifier = Guid.NewGuid(), Reference = "Test1", PaymentId = "PaymentId1" },
                        new Payment() { Identifier = Guid.NewGuid(), Reference = "Test2", PaymentId = "PaymentId2" }
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

            _mockGovUKPayApiClientFactory.Setup(x => x.Invoke(It.IsAny<string>()))
                .Returns(_mockGovUKPayApiClient.Object);

            var paymentState = Newtonsoft.Json.JsonConvert.DeserializeObject<PaymentState>("{ \"code\":\"test\", \"finished\": true, \"message\":\"method\", \"status\":\"success\" }");

            _mockGovUKPayApiClient.Setup(x => x.GetAPaymentAsync(
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
                    null,
                    null,
                    paymentState
                    ));

            _mockPaymentRepository.Setup(x => x.Update(It.IsAny<Payment>()))
                .ReturnsAsync(new OperationResult<Payment>(true) { Data = new Payment() { Identifier = Guid.NewGuid(), PaymentId = "paymentId", Reference = "refernce" } });

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsProcessFeeAsync(
                It.IsAny<string>(), 
                It.IsAny<ProcessFeeModel>(),
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
            result.Should().BeOfType<ProcessUncapturedPaymentsCommandResult>();
            result.TotalIdentified.Should().Be(2);
            result.TotalMarkedAsCaptured.Should().Be(0);
            result.TotalErrors.Should().Be(2);
        }

        [Fact]
        public async Task Handle_returns_ProcessUncapturedPaymentsCommandResult_when_successful()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Should().BeOfType<ProcessUncapturedPaymentsCommandResult>();
            result.TotalIdentified.Should().Be(2);
            result.TotalMarkedAsCaptured.Should().Be(2);
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

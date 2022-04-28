using Application.Commands;
using Application.Data;
using Application.Entities;
using Application.Result;
using Domain.Exceptions;
using FluentAssertions;
using GovUKPayApiClient.Model;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Command = Application.Commands.PaymentResponseCommand;
using Handler = Application.Commands.PaymentResponseCommandHandler;

namespace Application.UnitTests.Commands.PaymentResponse
{
    public class HandleTests
    {
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<Func<string, GovUKPayApiClient.Api.ICardPaymentsApi>> _mockGovUKPayApiClientFactory = new Mock<Func<string, GovUKPayApiClient.Api.ICardPaymentsApi>>();
        private readonly Mock<IAsyncRepository<Payment>> _mockPaymentRepository = new Mock<IAsyncRepository<Payment>>();
        private readonly Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi> _mockPendingTransactionsApi = new Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi>();
        private readonly Mock<LocalGovImsApiClient.Api.IFundMetadataApi> _mockFundMetadataApi = new Mock<LocalGovImsApiClient.Api.IFundMetadataApi>();

        private readonly Mock<GovUKPayApiClient.Api.ICardPaymentsApi> _mockGovUKPayApiClient = new Mock<GovUKPayApiClient.Api.ICardPaymentsApi>();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockGovUKPayApiClientFactory.Object,
                _mockPaymentRepository.Object,
                _mockPendingTransactionsApi.Object,
                _mockFundMetadataApi.Object);

            SetupClient();
            SetupCommand(Guid.NewGuid().ToString());
        }

        private void SetupClient()
        {
            _mockPaymentRepository.Setup(x => x.Get(It.IsAny<Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(new OperationResult<Payment>(true) { Data = new Payment() { Identifier = Guid.NewGuid(), Reference = "Test" } });

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

            _mockGovUKPayApiClientFactory.Setup(x => x.Invoke(It.IsAny<string>()))
                .Returns(_mockGovUKPayApiClient.Object);

            var paymentState = Newtonsoft.Json.JsonConvert.DeserializeObject<PaymentState>("{ \"code\":\"test\", \"finished\": true, \"message\":\"method\", \"status\":\"success\" }");

            _mockGovUKPayApiClient.Setup(x => x.GetAPaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetPaymentResult(
                    null,
                    0,
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

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsProcessPaymentAsync(It.IsAny<string>(), It.IsAny<ProcessPaymentModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessPaymentResponse());
        }

        private void SetupCommand(string paymentId)
        {
            _command = new Command() { PaymentId = paymentId };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("invalid payemnt id")]
        [InlineData("zzzzzzzz-e358-4cbb-97ed-467ad02ddd6a")]
        public async Task Handle_throws_PaymentException_when_request_is_not_valid(string paymentId)
        {
            // Arrange
            SetupCommand(paymentId);

            // Act
            async Task task() => await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("Unable to process the payment");
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
        public async Task Handle_returns_ProcessPaymentResponse_when_successful()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Should().BeOfType<PaymentResponseCommandResult>();
        }
    }
}

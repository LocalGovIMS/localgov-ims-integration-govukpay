//using Application.Clients.LocalGovImsPaymentApi;
//using Application.Commands;
//using Domain.Exceptions;
//using FluentAssertions;
//using Microsoft.Extensions.Configuration;
//using Moq;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Xunit;
//using Command = Application.Commands.PaymentResponseCommand;
//using Handler = Application.Commands.PaymentResponseCommandHandler;
//using Keys = Application.Commands.PaymentResponseParameterKeys;

//namespace Application.UnitTests.Commands.PaymentResponse
//{
//    public class HandleTests
//    {
//        private readonly Handler _commandHandler;
//        private Command _command;

//        private readonly Mock<IConfiguration> _mockConfiguration = new Mock<IConfiguration>();
//        private readonly Mock<ILocalGovImsPaymentApiClient> _mockLocalGovImsPaymentApiClient = new Mock<ILocalGovImsPaymentApiClient>();

//        public HandleTests()
//        {
//            _commandHandler = new Handler(
//                _mockConfiguration.Object,
//                _mockLocalGovImsPaymentApiClient.Object);

//            SetupConfig();
//            SetupClient(System.Net.HttpStatusCode.OK);
//            SetupCommand(new Dictionary<string, string> {
//                { Keys.AuthorisationResult, AuthorisationResult.Authorised },
//                { Keys.MerchantSignature, "NZL0OxbvIzufD/ejZODSJ3SzcNQKMJ1JhzQaKH9LWtM=" },
//                { Keys.PspReference, "8816281505278071" },
//                { Keys.PaymentMethod, "Card" }
//            });
//        }

//        private void SetupConfig()
//        {
//            var hmacKeyConfigSection = new Mock<IConfigurationSection>();
//            hmacKeyConfigSection.Setup(a => a.Value).Returns("FC81CC7410D19B75B6513FF413BE2E2762CE63D25BA2DFBA63A3183F796530FC");

//            _mockConfiguration.Setup(x => x.GetSection("SmartPay:HmacKey")).Returns(hmacKeyConfigSection.Object);
//        }

//        private void SetupClient(System.Net.HttpStatusCode statusCode)
//        {
//            _mockLocalGovImsPaymentApiClient.Setup(x => x.ProcessPayment(It.IsAny<string>(), It.IsAny<ProcessPaymentModel>()))
//                .ReturnsAsync(new ProcessPaymentResponseModel());
//        }

//        private void SetupCommand(Dictionary<string, string> parameters)
//        {
//            _command = new Command() { Paramaters = parameters };
//        }

//        [Fact]
//        public async Task Handle_throws_PaymentException_when_request_is_not_valid()
//        {
//            // Arrange
//            SetupCommand(new Dictionary<string, string> {
//                { Keys.AuthorisationResult, AuthorisationResult.Authorised },
//                { Keys.MerchantSignature, "1NZL0OxbvIzufD/ejZODSJ3SzcNQKMJ1JhzQaKH9LWtM=" },
//                { Keys.PspReference, "8816281505278071" },
//                { Keys.PaymentMethod, "Card" }
//            });

//            // Act
//            async Task task() => await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

//            // Assert
//            var result = await Assert.ThrowsAsync<PaymentException>(task);
//            result.Message.Should().Be("Unable to process the payment");
//        }

//        [Theory]
//        [InlineData(AuthorisationResult.Authorised, "NZL0OxbvIzufD/ejZODSJ3SzcNQKMJ1JhzQaKH9LWtM=")]
//        [InlineData("Another value", "97Y0KDL1+KEe0gTQJzQ/mBQJIj1dTsIubOwItb+Hsx0=")]
//        public async Task Handle_returns_a_ProcessPaymentResponseModel(string authorisationResult, string merchantSignature)
//        {
//            // Arrange
//            SetupCommand(new Dictionary<string, string> {
//                { Keys.AuthorisationResult, authorisationResult },
//                { Keys.MerchantSignature, merchantSignature },
//                { Keys.PspReference, "8816281505278071" },
//                { Keys.PaymentMethod, "Card" }
//            });

//            // Act
//            var result = await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

//            // Assert
//            result.Should().BeOfType<ProcessPaymentResponseModel>();
//        }
//    }
//}

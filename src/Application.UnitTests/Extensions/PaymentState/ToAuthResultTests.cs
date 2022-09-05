using FluentAssertions;
using Xunit;
using Domain;
using Application.Extensions;

namespace Application.UnitTests.Extensions.PaymentState
{
    public class ToAuthResultTests
    {
        [Theory]
        [InlineData(PaymentStatus.Created, AuthResult.Pending)]
        [InlineData(PaymentStatus.Started, AuthResult.Pending)]
        [InlineData(PaymentStatus.Capturable, AuthResult.Pending)]
        [InlineData(PaymentStatus.Success, AuthResult.Authorised)]
        [InlineData(PaymentStatus.Failed, AuthResult.Refused)]
        [InlineData(PaymentStatus.Cancelled, AuthResult.Error)]
        [InlineData(PaymentStatus.Error, AuthResult.Error)]
        public void ToAuthResult_returns_the_expectedAuthResult(string status, string expectedResult)
        {
            // Arrange
            var deserialisedPaymentState = Newtonsoft.Json.JsonConvert.DeserializeObject<GovUKPayApiClient.Model.PaymentState>($"{{\"status\":\"{status}\"}}");

            // Act
            var result = deserialisedPaymentState.ToAuthResult();

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}

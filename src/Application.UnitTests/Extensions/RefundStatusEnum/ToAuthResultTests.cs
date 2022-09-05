using FluentAssertions;
using Xunit;
using Domain;
using Application.Extensions;

namespace Application.UnitTests.Extensions.RefundStatusEnum
{
    public class ToAuthResultTests
    {
        [Theory]
        [InlineData(GovUKPayApiClient.Model.Refund.StatusEnum.Success, AuthResult.Authorised)]
        [InlineData(GovUKPayApiClient.Model.Refund.StatusEnum.Submitted, AuthResult.Pending)]
        [InlineData(GovUKPayApiClient.Model.Refund.StatusEnum.Error, AuthResult.Error)]
        [InlineData(null, AuthResult.Error)]
        public void ToAuthResult_returns_the_expectedAuthResult(GovUKPayApiClient.Model.Refund.StatusEnum? status, string expectedResult)
        {
            // Arrange
            
            // Act
            var result = status.ToAuthResult();

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}

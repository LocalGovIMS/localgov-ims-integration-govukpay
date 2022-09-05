using FluentAssertions;
using Xunit;
using Application.Extensions;

namespace Application.UnitTests.Extensions.RefundStatusEnum
{
    public class IsFinishedTests
    {
        [Theory]
        [InlineData(GovUKPayApiClient.Model.Refund.StatusEnum.Success, true)]
        [InlineData(GovUKPayApiClient.Model.Refund.StatusEnum.Submitted, false)]
        [InlineData(GovUKPayApiClient.Model.Refund.StatusEnum.Error, true)]
        [InlineData(null, false)]
        public void IsFinished_returns_the_expectedAuthResult(GovUKPayApiClient.Model.Refund.StatusEnum? status, bool expectedResult)
        {
            // Arrange
            
            // Act
            var result = status.IsFinished();

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}

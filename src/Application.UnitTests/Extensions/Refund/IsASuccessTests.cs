using FluentAssertions;
using Xunit;
using Application.Extensions;

namespace Application.UnitTests.Extensions.Refund
{
    public class IsASuccessTests
    {
        [Fact]
        public void IsFinished_when_called_for_a_successful_refund_returns_true()
        {
            // Arrange
            var refund = TestData.GetSuccessfulRefund();
            
            // Act
            var result = refund.IsASuccess();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsFinished_when_called_for_a_submitted_refund_returns_false()
        {
            // Arrange
            var refund = TestData.GetSubmittedRefund();

            // Act
            var result = refund.IsASuccess();

            // Assert
            result.Should().BeFalse();
        }
    }
}

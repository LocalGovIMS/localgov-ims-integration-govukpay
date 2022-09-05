using FluentAssertions;
using Xunit;
using Application.Extensions;

namespace Application.UnitTests.Extensions.Refund
{
    public class FailTests
    {
        [Fact]
        public void Fail_sets_the_status_to_error()
        {
            // Arrange
            var refund = TestData.GetSubmittedRefund();

            // Act
            refund.Fail();

            // Assert
            refund.Status.Should().Be(GovUKPayApiClient.Model.Refund.StatusEnum.Error.ToEnumMemberValue());
        }

        [Fact]
        public void Fail_adds_a_StatusHistory_entry()
        {
            // Arrange
            var refund = TestData.GetSubmittedRefund();
            var existingStatusHistoryCount = refund.StatusHistory.Count;

            // Act
            refund.Fail();

            // Assert
            refund.StatusHistory.Count.Should().Be(existingStatusHistoryCount + 1);
        }
    }
}

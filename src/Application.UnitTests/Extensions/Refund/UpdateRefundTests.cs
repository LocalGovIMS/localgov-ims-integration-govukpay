using FluentAssertions;
using Xunit;
using Application.Extensions;

namespace Application.UnitTests.Extensions.Refund
{
    public class UpdateRefundTests
    {
        [Fact]
        public void Update_sets_the_RefundId()
        {
            // Arrange
            var refundId = "TestRefundId";
            var refund = TestData.GetSubmittedRefund();
            var refundResult = TestData.GetSuccessfulRefundResult(refundId);

            // Act
            refund.Update(refundResult);

            // Assert
            refund.RefundId.Should().Be(refundId);
        }

        [Fact]
        public void Update_sets_the_status()
        {
            // Arrange
            var refundId = "TestRefundId";
            var refund = TestData.GetSubmittedRefund();
            var refundResult = TestData.GetSuccessfulRefundResult(refundId);

            // Act
            refund.Update(refundResult);

            // Assert
            refund.Status.Should().Be(GovUKPayApiClient.Model.Refund.StatusEnum.Success.ToEnumMemberValue());
        }
    }
}

using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Result.OperationResult
{
    public class ConstructorBoolTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Success_returns_the_value_specified_in_the_constructor(bool success)
        {
            // Arrange
            var operationResult = new Application.Result.OperationResult<object>(success);

            // Act
            var result = operationResult.Success;

            // Assert
            result.Should().Be(success);
        }
    }
}

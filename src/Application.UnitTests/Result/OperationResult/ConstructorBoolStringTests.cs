using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Result.OperationResult
{
    public class ConstructorBoolStringTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Success_returns_the_value_specified_in_the_constructor(bool success)
        {
            // Arrange
            var operationResult = new Application.Result.OperationResult<object>(success, "error");

            // Act
            var result = operationResult.Success;

            // Assert
            result.Should().Be(success);
        }

        [Theory]
        [InlineData("An error")]
        [InlineData("Another error")]
        public void Errors_returns_a_list_with_one_entry(string error)
        {
            // Arrange
            var operationResult = new Application.Result.OperationResult<object>(true, error);

            // Act
            var result = operationResult.Errors;

            // Assert
            result.Count.Should().Be(1);
            result[0].Should().Be(error);
        }
    }
}

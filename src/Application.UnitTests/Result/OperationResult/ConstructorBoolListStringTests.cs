using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Application.UnitTests.Result.OperationResult
{
    public class ConstructorBoolListStringTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Success_returns_the_value_specified_in_the_constructor(bool success)
        {
            // Arrange
            var operationResult = new Application.Result.OperationResult<object>(success, new List<string> { "An error", "Enother error" });

            // Act
            var result = operationResult.Success;

            // Assert
            result.Should().Be(success);
        }

        [Theory]
        [InlineData(new[] { "Error" }, 1)]
        [InlineData(new[] { "Error 1", "Error 2" }, 2)]
        [InlineData(new[] { "An error", "Another error", "Yet another error" }, 3)]
        public void Errors_returns_a_list_with_the_expected_number_or_entries(string[] errors, int expectedNumberOfEntries)
        {
            // Arrange
            var operationResult = new Application.Result.OperationResult<object>(true, errors);

            // Act
            var result = operationResult.Errors;

            // Assert
            result.Count.Should().Be(expectedNumberOfEntries);
        }
    }
}

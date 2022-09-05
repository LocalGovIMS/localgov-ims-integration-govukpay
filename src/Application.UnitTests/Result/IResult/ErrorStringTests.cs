using FluentAssertions;
using Moq;
using Xunit;

namespace Application.UnitTests.Result.IResult
{
    public class ErrorStringTests
    {
        [Theory]
        [InlineData(new[] { "Error 1", "Error 2" }, "Error 1, Error 2")]
        [InlineData(new[] { "An error", "Another error", "Yet another error" }, "An error, Another error, Yet another error")]
        public void ErrorString_returns_the_combined_value_or_all_errors(string[] errors, string expectedResult)
        {
            // Arrange
            var mockResult = new Mock<Application.Result.IResult>
            {
                CallBase = true
            };

            mockResult.SetupGet(m => m.Errors).Returns(errors);
            
            // Act
            var result = mockResult.Object.ErrorString;

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}

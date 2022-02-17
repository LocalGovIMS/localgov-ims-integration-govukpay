using Application.Extensions;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Extensions.Decimal
{
    public class ToPenceTests
    {
        [Theory]
        [InlineData(1.1D, 110)]
        [InlineData(0.99D, 99)]
        [InlineData(0.555D, 55)]
        [InlineData(100.11D, 10011)]

        public void ToPence_converts_a_decimal_number_representing_a_value_in_pounds_to_pence(decimal value, int expectedPence)
        {
            // Arrange

            // Act
            var result = value.ToPence();

            // Assert
            result.Should().Be(expectedPence);
        }
    }
}

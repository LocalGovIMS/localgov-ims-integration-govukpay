using Application.Extensions;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Extensions.Enum
{
    public class ToEnumMemberValueTests
    {
        [Theory]
        [InlineData(TestData.TestEnum.Success, "success")]
        [InlineData(TestData.TestEnum.Submitted, "submitted")]
        [InlineData(TestData.TestEnum.Error, "error")]
        [InlineData(TestData.TestEnum.NoEnumMemberValue, "NoEnumMemberValue")]
        public void ToEnumMemberValue_returns_the_value_of_the_enum_member_or_the_name_of_the_option_if_no_EnumMember_attribute_is_used(TestData.TestEnum enumValue, string expectedResult)
        {
            // Arrange

            // Act
            var result = enumValue.ToEnumMemberValue();

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}

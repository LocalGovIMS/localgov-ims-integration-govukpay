using Application.Cryptography;
using FluentAssertions;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Application.UnitTests.Cryptography.MD5CryptographyService
{
    [ExcludeFromCodeCoverage]
    public class GetHashTests
    {
        private readonly ICryptographyService _cryptographyService;

        public GetHashTests()
        {
            _cryptographyService = new Application.Cryptography.MD5CryptographyService();
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void GetHash_should_not_be_null(string source, string expectedResult)
        {
            // Arrange

            // Act
            var result = _cryptographyService.GetHash(source);

            // Assert
            result.Should().NotBeNullOrEmpty(result);
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void GetHash_should_be_a_string(string source, string expectedResult)
        {
            // Arrange

            // Act
            var result = _cryptographyService.GetHash(source);

            // Assert
            result.Should().BeAssignableTo<string>();
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void GetHash_should_be_the_expected_hash(string source, string expectedResult)
        {
            // Arrange

            // Act
            var result = _cryptographyService.GetHash(source);

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}

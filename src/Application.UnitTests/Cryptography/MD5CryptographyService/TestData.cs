using System.Collections;
using System.Collections.Generic;

namespace Application.UnitTests.Cryptography.MD5CryptographyService
{
    public class TestData : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new()
        {
            new object[] { "1234567890", "e807f1fcf82d132f9bb018ca6738a19f" },
            new object[] { "ABCDEFGHIJ", "e86410fa2d6e2634fd8ac5f4b3afe7f3" },
            new object[] { "12345ABCDE", "fc85a7ce091aea86ef3463b9166e9b06" },
            new object[] { "A1B2C3D4E4", "23d9fbff3779ee284004c22a16fee960" },
            new object[] { "abcdefghij", "a925576942e94b2ef57a066101b48876" },
            new object[] { "^%$-*^*_£&", "17c04f9fe92698077109382048883c3c" },
            new object[] { "ALongStringWithNoSpacesThatImJustUsingForTestPurposes", "f4e2f1c69c3840c9ab03bc4dfde5e392" },
            new object[] { "HTRH TWJ4ij jioy42io jihrH WRH6j iw2w 4 byw h^$ UJu46£%£u6", "b0f89957e8425e88212490986d9ec755" }
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

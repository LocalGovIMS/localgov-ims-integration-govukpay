using System.Runtime.Serialization;

namespace Application.UnitTests.Extensions.Enum
{
    public class TestData
    {
        public enum TestEnum
        {
            //
            // Summary:
            //     Enum Submitted for value: submitted
            [EnumMember(Value = "submitted")]
            Submitted = 1,

            //
            // Summary:
            //     Enum Success for value: success
            [EnumMember(Value = "success")]
            Success,

            //
            // Summary:
            //     Enum Error for value: error
            [EnumMember(Value = "error")]
            Error,

            NoEnumMemberValue
        }
    }
}

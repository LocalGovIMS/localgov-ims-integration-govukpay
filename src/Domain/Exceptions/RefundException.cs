using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Domain.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class RefundException : Exception
    {
        public RefundException()
        {
        }

        public RefundException(string message)
            : base(message)
        {
        }

        public RefundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RefundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

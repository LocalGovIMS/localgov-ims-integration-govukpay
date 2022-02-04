using System.Collections.Generic;

namespace Application.Result
{
    public interface IResult : IResult<object>
    {
    }

    public interface IResult<T>
    {
        bool Success { get; }
        IList<string> Errors { get; }
        string ErrorString => string.Join(", ", Errors);
        T Data { get; }
    }
}

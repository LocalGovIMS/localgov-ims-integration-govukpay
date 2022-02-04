using Application.Data;
using System.Collections.Generic;

namespace Application.Result
{
    public class OperationResult<T> : IResult<T>
    {
        public OperationResult(bool success)
        {
            Success = success;
        }

        public OperationResult(bool success, string error)
        {
            Success = success;
            Errors.Add(error);
        }

        public OperationResult(bool success, IList<string> errors)
        {
            Success = success;
            Errors = errors;
        }

        public bool Success { get; }
        public IList<string> Errors { get; set; } = new List<string>();
        public T Data { get; set; }
    }
}

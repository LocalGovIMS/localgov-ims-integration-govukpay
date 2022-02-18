using Application.Entities;
using Application.Result;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Data
{
    public interface IAsyncRepository<T> where T : BaseEntity
    {
        Task<IResult<T>> Get(Expression<Func<T, bool>> criteria);
        Task<IResult<List<T>>> List(Expression<Func<T, bool>> criteria);
        Task<IResult<T>> Add(T entity);
        Task<IResult<T>> Update(T entity);
    }
}

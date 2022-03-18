using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Application.Data
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>> Criteria { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        List<string> IncludeStrings { get; }
        Expression<Func<T, object>> Order { get; }
        Expression<Func<T, object>> OrderDesc { get; }
        int? Count { get; }
        bool IsSatisfiedBy(T type);
        void AddOrder(Expression<Func<T, object>> orderExpression);
        void AddOrderDesc(Expression<Func<T, object>> orderExpression);
    }
}

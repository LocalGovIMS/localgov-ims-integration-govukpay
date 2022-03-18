using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Application.Data
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public Expression<Func<T, object>> GroupBy { get; private set; }
        public Expression<Func<T, bool>> Criteria { get; }
        public List<Expression<Func<T, object>>> Includes { get; } = new List<Expression<Func<T, object>>>();
        public List<string> IncludeStrings { get; } = new List<string>();
        public Expression<Func<T, object>> Order { get; private set; }
        public Expression<Func<T, object>> OrderDesc { get; private set; }
        public int? Count { private set; get; }

        public bool IsSatisfiedBy(T item)
        {
            var items = new List<T>
            {
                item
            }.AsQueryable();

            return items.Where(Criteria).Any();
        }

        public virtual void AddOrder(Expression<Func<T, object>> orderExpression)
        {
            Order = orderExpression;
        }

        public virtual void AddOrderDesc(Expression<Func<T, object>> orderExpression)
        {
            OrderDesc = orderExpression;
        }

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        public virtual void AddGroupBy(Expression<Func<T, object>> groupByExpression)
        {
            GroupBy = groupByExpression;
        }

        public virtual void AddCount(int count)
        {
            Count = count;
        }
    }
}

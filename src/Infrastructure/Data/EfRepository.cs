using Application.Data;
using Application.Entities;
using Application.Result;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class EfRepository<T> : IAsyncRepository<T> where T : BaseEntity
    {
        protected readonly GovUkPayDbContext DbContext;

        public EfRepository(GovUkPayDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public virtual async Task<IResult<T>> Get(Expression<Func<T, bool>> criteria)
        {
            var entity = await DbContext.Set<T>().AsQueryable().FirstOrDefaultAsync(criteria);

            return new OperationResult<T>(true) { Data = entity };
        }

        public virtual async Task<IResult<List<T>>> List(Expression<Func<T, bool>> criteria)
        {
            var results = DbContext.Set<T>().AsQueryable().Where(criteria);

            return new OperationResult<List<T>>(true) { Data = await results.ToListAsync()};
        }

        public virtual async Task<IResult<T>> Single(ISpecification<T> spec, bool tracking = false)
        {
            var list = BuildQueryableUsingSpec(spec, tracking);

            return new OperationResult<T>(true) { Data = await list.SingleOrDefaultAsync() };
        }

        public virtual async Task<IResult<T>> First(ISpecification<T> spec, bool tracking = false)
        {
            var list = BuildQueryableUsingSpec(spec, tracking);

            return new OperationResult<T>(true) { Data = await list.FirstOrDefaultAsync() };
        }

        public virtual async Task<IResult<List<T>>> List(ISpecification<T> spec, bool tracking = false)
        {
            var result = BuildQueryableUsingSpec(spec, false);

            return new OperationResult<List<T>>(true) { Data = await result.ToListAsync() };
        }

        private IQueryable<T> BuildQueryableUsingSpec(ISpecification<T> spec, bool tracking = false)
        {
            // fetch a Queryable that includes all expression-based includes
            var queryableResultWithIncludes = spec.Includes
                .Aggregate(DbContext.Set<T>().AsQueryable(),
                    (current, include) => current.Include(include));

            // modify the IQueryable to include any string-based include statements
            var secondaryResult = spec.IncludeStrings
                .Aggregate(queryableResultWithIncludes,
                    (current, include) => current.Include(include));

            // add order statements to result
            if (spec.Order != null) secondaryResult = secondaryResult.OrderBy(spec.Order);

            if (spec.OrderDesc != null) secondaryResult = secondaryResult.OrderByDescending(spec.OrderDesc);

            if (spec.Count != null) secondaryResult.Take(spec.Count.Value);

            if (tracking) secondaryResult = secondaryResult.AsNoTracking();

            return secondaryResult.Where(spec.Criteria);
        }

        public virtual async Task<IResult<T>> Add(T entity)
        {
            try
            {
                await DbContext.Set<T>().AddAsync(entity);
                await DbContext.SaveChangesAsync();

                return new OperationResult<T>(true) { Data = entity };
            }

            catch (Exception e)
            {
                Log.Error(e, "Error adding entity");
                return new OperationResult<T>(false, e.Message);
            }
        }

        public virtual async Task<IResult<T>> Update(T entity)
        {
            try
            {
                DbContext.Set<T>().Update(entity);
                await DbContext.SaveChangesAsync();

                return new OperationResult<T>(true) { Data = entity };
            }
            catch (Exception e)
            {
                Log.Error(e, "Error updating entity");
                return new OperationResult<T>(false, e.Message);
            }
        }
    }
}

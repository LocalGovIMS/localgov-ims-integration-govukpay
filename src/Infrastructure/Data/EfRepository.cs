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

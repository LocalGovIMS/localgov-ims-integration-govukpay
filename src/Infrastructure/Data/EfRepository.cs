using Application.Data;
using Application.Entities;
using Application.Result;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
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

        public virtual async Task<IResult<T>> AddAsync(T entity)
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

        public virtual async Task<IResult<T>> UpdateAsync(T entity)
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

using BackPredictFinance.Datas.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Datas.Common
{
	// Je t'aime ChatGPT
	// Extension method to concatenate predicate expressions
	public static class PredicateExtensions
	{
		public static Expression<Func<T, bool>> AndAlso<T>(
			this Expression<Func<T, bool>> expr1,
			Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>(
				Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
		}
	}

    public class DBTools<TEntity> where TEntity : class
    {
        public DBTools()
        {
        }

        public void detachIfNeeded(TEntity entity, FinanceDbContext _context)
        {
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
            {
                return;
            }

            var entityId = idProperty.GetValue(entity);
            var local = _context.Set<TEntity>().Local.FirstOrDefault(entry => Equals(idProperty.GetValue(entry), entityId));
            if (local != null)
            {
                _context.Entry(local).State = EntityState.Detached;
            }
        }
    }

    public static class DbSetExtensions
    {
        /// <summary>
        /// Returns the <see cref="entities"/> with included paths.
        /// </summary>
        /// <typeparam name="T">Type of the entities</typeparam>
        /// <param name="entities">The entities</param>
        /// <param name="includes">The paths to include</param>
        /// <returns></returns>
        public static IQueryable<T> SetupIncludes<T>(this DbSet<T> entities, IEnumerable<string>? includes = null)
            where T : class
        {
            if (includes == null || !includes.Any()) return entities;
            var dbSet = entities.AsQueryable();
            return includes.Aggregate(dbSet, (current, include) => current.Include(include));
        }
    }
}

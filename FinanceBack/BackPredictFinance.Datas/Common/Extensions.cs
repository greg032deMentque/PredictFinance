using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackPredictFinance.Datas.Common
{
    public static class QueryableOrderExtensions
    {
        public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> source, string sortActive, bool desc)
        {
            if (string.IsNullOrWhiteSpace(sortActive)) return source;

            var param = Expression.Parameter(typeof(T), "x");

            Expression body = param;
            foreach (var member in sortActive.Split('.', StringSplitOptions.RemoveEmptyEntries))
                body = Expression.PropertyOrField(body, member);

            var keySelector = Expression.Lambda(body, param);

            var method = desc ? "OrderByDescending" : "OrderBy";
            var call = Expression.Call(
                typeof(Queryable),
                method,
                [typeof(T), body.Type],
                source.Expression,
                Expression.Quote(keySelector)
            );

            return source.Provider.CreateQuery<T>(call);
        }
    }

}

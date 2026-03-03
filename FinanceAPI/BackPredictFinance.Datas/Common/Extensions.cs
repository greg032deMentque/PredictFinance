using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;

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

    public static class Extentions    
    {
        /// <summary>
        /// Gets n elements in the table T order by a property.
        /// </summary>
        /// <param name="start">Start line of the pagination.</param>
        /// <param name="take"></param>
        /// <param name="sortName">Sortable column name.</param>
        /// <param name="sortDir">Direction of the sort.</param>
        /// <param name="predicate">[Optional] Filter predicate to apply to the search.</param>
        /// <param name="includes">[Optional] Tables to include.</param>
        /// <returns>The elements by pagination.</returns>
        public static async Task<List<TEntity>> GetByPaginationAsync<TEntity>(this DbSet<TEntity> _dbSet, int start, int take, string sortName, bool sortDir,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null, List<Guid>? countryIds = null) where TEntity : class
        {
            var query = _dbSet.AsQueryable();

            if (include != null)
            {
                query = include(query);
            }

            var items = predicate != null ? query.Where(predicate) : query;

            var sortNameArray = sortName.Split(',');

            // If multiple column sort
            if (sortNameArray.Count() > 1)
            {
                for (var i = 0; i < sortNameArray.Count(); i++)
                {
                    sortNameArray[i] += sortDir ? " descending" : string.Empty;
                }

                sortName = string.Join(",", sortNameArray);
            }

            var itmDalList = string.IsNullOrWhiteSpace(sortName) ?
                items.Skip(start).Take(take) :
                items.OrderBy(sortName + (sortDir ? " descending" : string.Empty)).Skip(start).Take(take);

            return await itmDalList.ToListAsync();
        }

        public static async Task<int> GetTotalCountAsync<TEntity>(this DbSet<TEntity> _dbSet, Expression<Func<TEntity, bool>> predicate = null,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null, List<Guid>? countryIds = null) where TEntity : class
        {
            var query = _dbSet.AsQueryable();

            if (include != null)
            {
                query = include(query);
            }

            return await (predicate != null ? query.CountAsync(predicate) : query.CountAsync());
        }
    }


    public static class DeterministicGuid
    {
        public static Guid Create(Guid namespaceId, string name)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var namespaceBytes = namespaceId.ToByteArray();

            SwapByteOrder(namespaceBytes);

            var hashInput = namespaceBytes.Concat(nameBytes).ToArray();
            var hash = sha1.ComputeHash(hashInput);

            var newGuid = new byte[16];
            Array.Copy(hash, 0, newGuid, 0, 16);

            // Set the variant and version bits as per RFC 4122
            newGuid[6] = (byte)(newGuid[6] & 0x0F | 5 << 4); // Version 5
            newGuid[8] = (byte)(newGuid[8] & 0x3F | 0x80);     // Variant is RFC 4122

            SwapByteOrder(newGuid);
            return new Guid(newGuid);
        }

        private static void SwapByteOrder(byte[] guid)
        {
            void Swap(int a, int b)
            {
                byte tmp = guid[a];
                guid[a] = guid[b];
                guid[b] = tmp;
            }

            Swap(0, 3);
            Swap(1, 2);
            Swap(4, 5);
            Swap(6, 7);
        }
    }

}

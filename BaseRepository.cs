using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PureIP.Portal.Domain.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PureIP.Portal.Domain.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using PureIP.Portal.Data.Logging;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace PureIP.Portal.Data
{
    /// <summary>
    /// base repository using the unit of work pattern with one context
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public abstract class BaseRepository<TContext> : IBaseRepository where TContext : DbContext
    {
        private readonly IServiceProvider _container;
        private readonly Lazy<ILogger> _logger;
        private readonly Lazy<TContext> _context;
        private readonly Lazy<LoggingContext> _loggingcontext;
        private readonly Lazy<HttpContext> _httpContext;
        private readonly string connectionString;

        protected ILogger logger => _logger.Value;
        protected TContext context => _context.Value;
        private LoggingContext loggingContext => _loggingcontext.Value;

        protected ClaimsPrincipal user => _httpContext.Value.User;


        public BaseRepository(IServiceProvider container)
        {
            _container = container;
            _context = new Lazy<TContext>(() => CreateContext());
            _loggingcontext = new Lazy<LoggingContext>(() => _container.GetService<LoggingContext>());
            connectionString = context.Database.GetDbConnection().ConnectionString;
            var logType = typeof(ILogger<>).MakeGenericType(this.GetType());
            _httpContext = new Lazy<HttpContext>(() => _container.GetService<IHttpContextAccessor>().HttpContext);
            _logger = new Lazy<ILogger>(() => _container.GetService(logType) as ILogger);
        }

        protected TContext CreateContext()
        {
            return _container.GetService<TContext>();
        }

        public T Find<T>(object Id) where T : class
        {
            return context.Set<T>().Find(Id);
        }

        public virtual T Get<T>(Expression<Func<T, bool>> predicate, string includes = "") where T : class
        {
            var query = context.Set<T>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(includes))
                foreach (var include in includes.Split(','))
                {
                    query = query.Include(include);
                }
            return query.FirstOrDefault(predicate);
        }

        public T Get<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, System.Collections.IEnumerable>> navigationPropertyPath) where T : class
        {
            var query = navigationPropertyPath == null ? context.Set<T>().AsNoTracking() : context.Set<T>().Include(navigationPropertyPath).AsNoTracking();
            return query.FirstOrDefault(predicate);
        }

        public int Count<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var query = context.Set<T>().AsNoTracking();
            if (predicate != null)
                query = query.Where(predicate);
            return query.Count();
        }

        //public Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        //{
        //    using (var context = CreateContext())
        //    {
        //        return context.Set<T>().FirstOrDefaultAsync(predicate);
        //    }
        //}

        public List<T> GetList<T>(Expression<Func<T, bool>> predicate = null, Expression<Func<T, IComparable>> orderBy = null, string includes = "") where T : class
        {
            var query = context.Set<T>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(includes))
                foreach (var include in includes.Split(','))
                {
                    query = query.Include(include);
                }
            if (predicate != null)
                query = query.Where(predicate);
            if (orderBy != null)
                query = query.OrderBy(orderBy);
            return query.ToList();
        }

        public async Task<PaginatedList<TItem, TSearch>> GetPaginatedAsync<TItem, TSearch>(
            IPaginatedList<TSearch> page,
            List<Expression<Func<TItem, bool>>> predicates,            
            string includes = "",
            Expression<Func<TItem, decimal>> summaryValue = null) where TItem : class
        {
            var query = context.Set<TItem>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(includes))
                foreach (var include in includes.Split(','))
                {
                    query = query.Include(include);
                }
            if (predicates != null)
                foreach (var predicate in predicates)
                    query = query.Where(predicate);
            query = query.OrderBy(page.OrderBy, !page.OrderByDesc);
            decimal? summary = null;
            if (summaryValue != null)
                summary = await query.SumAsync(summaryValue);
            var count = await query.CountAsync();
            var items = await query.Skip((page.PageIndex - 1) * page.PageSize).Take(page.PageSize).ToListAsync();
            return new PaginatedList<TItem, TSearch>(page, items, count, summary);
        }

        //public List<T> GetList<T>(Expression<Func<T, bool>> predicate = null, Expression<Func<T, IComparable>> orderBy = null, Expression<Func<T, System.Collections.IEnumerable>> navigationPropertyPath = null) where T : class
        //{
        //    var query = navigationPropertyPath == null ? context.Set<T>().AsNoTracking() : context.Set<T>().Include(navigationPropertyPath).AsNoTracking();
        //    if (predicate != null)
        //        query = query.Where(predicate);
        //    if (orderBy != null)
        //        query = query.OrderBy(orderBy);
        //    return query.ToList();
        //}

        public IQueryable<T> GetQuery<T>(Expression<Func<T, bool>> predicate = null, Expression<Func<T, IComparable>> orderBy = null) where T : class
        {
            var query = context.Set<T>().AsNoTracking();
            if (predicate != null)
                query = query.Where(predicate);
            if (orderBy != null)
                query = query.OrderBy(orderBy);
            return query;
        }

        //public Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> predicate = null, Expression<Func<T, IComparable>> orderBy = null) where T : class
        //{
        //    using (var context = CreateContext())
        //    {
        //        var query = context.Set<T>().AsNoTracking();
        //        if (predicate != null)
        //            query = query.Where(predicate);
        //        if (orderBy != null)
        //            query = query.OrderBy(orderBy);
        //        return query.ToListAsync();
        //    }
        //}

        public virtual void Add<T>(T item) where T : class
        {
            context.Set<T>().Add(item);
            Audit(item, Domain.Models.Logging.ActionType.Insert);
        }

        public void Add<T>(List<T> items) where T : class
        {
            context.Set<T>().AddRange(items);
            items.ForEach(item => Audit(item, Domain.Models.Logging.ActionType.Insert));
        }

        public virtual void Update<T>(T item) where T : class
        {
            context.Set<T>().Update(item);
            Audit(item, Domain.Models.Logging.ActionType.Update);
        }

        public void Update<T>(List<T> items) where T : class
        {
            context.Set<T>().UpdateRange(items);
            items.ForEach(item => Audit(item, Domain.Models.Logging.ActionType.Update));
        }

        public virtual void Delete<T>(T item) where T : class
        {
            context.Set<T>().Remove(item);
            Audit(item, Domain.Models.Logging.ActionType.Delete);
        }

        /// <summary>
        /// add or update the item, drops any navigation properties not included
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="include"></param>
        public void AddUpdate<T>(T item, params Type[] include) where T : class
        {
            context.ChangeTracker.TrackGraph(item, e =>
            {
                if (e.Entry.Entity is T || (include != null && include.Contains(e.Entry.Entity.GetType())))
                {
                    if (e.Entry.IsKeySet)
                    {
                        e.Entry.State = EntityState.Modified;
                        Audit(e.Entry.Entity, Domain.Models.Logging.ActionType.Update);
                    }
                    else
                    {
                        e.Entry.State = EntityState.Added;
                        Audit(e.Entry.Entity, Domain.Models.Logging.ActionType.Insert);
                    }
                }
            });
        }

        public int SaveChanges()
        {
            loggingContext.SaveChangesAsync();
            return context.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            loggingContext.SaveChangesAsync();
            return context.SaveChangesAsync();
        }

        private void Audit<T>(T data, Domain.Models.Logging.ActionType action)
        {
            loggingContext.Audit.AddAsync(new Domain.Models.Logging.Audit() { Action = action, Type = data.GetType().FullName, Data = Serialize(data), CreateDate = DateTime.UtcNow, CreatedBy = UserId(user) });
        }
        private Guid UserId(ClaimsPrincipal user)
        {
            if (user.Identity.IsAuthenticated)
            {
                var claim = user.Claims.FirstOrDefault(v => v.Type == ClaimTypes.NameIdentifier);
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                    return Guid.Parse(claim.Value);
            }
            return Guid.Empty;
        }
        private string Serialize<T>(T value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                var type = value.GetType();
                foreach (var prop in type.GetProperties())
                {
                    if ((prop.PropertyType.IsClass && prop.PropertyType.FullName != "System.String")
                        || (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        || prop.PropertyType.IsInterface)
                        overrides.Add(type, prop.Name, new XmlAttributes { XmlIgnore = true });
                }
                var xmlserializer = new XmlSerializer(value.GetType(), overrides);
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter))
                {
                    xmlserializer.Serialize(writer, value);
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Serialize");
                return string.Empty;
            }
        }

        protected DataTable CoreStoredProc(string storedProc, string action, string param)
        {
            System.Data.DataTable dt = new DataTable();
            System.Data.SqlClient.SqlDataAdapter da = new SqlDataAdapter(new SqlCommand("", new SqlConnection(connectionString)) { CommandText = storedProc, CommandType = CommandType.StoredProcedure });
            da.SelectCommand.Parameters.Add(new SqlParameter("Action", action));
            da.SelectCommand.Parameters.Add(new SqlParameter("Params", param));
            da.Fill(dt);
            return dt;
        }

        protected List<T> CoreStoredProc<T>(string storedProc, string action, string param) where T : class
        {
            return this.context.Set<T>().FromSql($"{storedProc} {action}, {param}").ToList();
        }

        public void BulkInsert<T>(List<T> data)
        {
            using (var sqlCopy = new System.Data.SqlClient.SqlBulkCopy(connectionString))
            {
                var metaData = context.Model.FindEntityType(typeof(T)).Relational();
                sqlCopy.DestinationTableName = $"{metaData.Schema}.{metaData.TableName}";
                sqlCopy.BatchSize = 500;

                var table = CreateDataTable(data);

                sqlCopy.WriteToServer(table);
            }
        }
        public static System.Data.DataTable CreateDataTable<T>(IEnumerable<T> list)
        {
            Type type = typeof(T);
            var properties = type.GetProperties();

            var dataTable = new System.Data.DataTable();
            foreach (var info in properties)
            {
                dataTable.Columns.Add(new System.Data.DataColumn(info.Name, Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType));
            }

            foreach (T entity in list)
            {
                object[] values = new object[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    values[i] = properties[i].GetValue(entity);
                }

                dataTable.Rows.Add(values);
            }

            return dataTable;
        }
    }

    public static class Extentions
    {
        /// <summary>
        /// Sorts the elements of a sequence according to a key and the sort order.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="query" />.</typeparam>
        /// <param name="query">A sequence of values to order.</param>
        /// <param name="key">Name of the property of <see cref="TSource"/> by which to sort the elements.</param>
        /// <param name="ascending">True for ascending order, false for descending order.</param>
        /// <returns>An <see cref="T:System.Linq.IOrderedQueryable`1" /> whose elements are sorted according to a key and sort order.</returns>
        public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> query, string key, bool ascending = true)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return query;
            }

            var lambda = (dynamic)CreateExpression(typeof(TSource), key);

            return ascending
                ? Queryable.OrderBy(query, lambda)
                : Queryable.OrderByDescending(query, lambda);
        }

        private static LambdaExpression CreateExpression(Type type, string propertyName)
        {
            var param = Expression.Parameter(type, "x");

            Expression body = param;
            foreach (var member in propertyName.Split('.'))
            {
                body = Expression.PropertyOrField(body, member);
            }

            return Expression.Lambda(body, param);
        }
    }
}

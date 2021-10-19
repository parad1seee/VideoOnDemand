using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace VideoOnDemand.DAL.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IDataContext _context;
        private DbSet<T> _entities;

        public Repository(IDataContext context)
        {
            _context = context;
        }

        private DbSet<T> Entities
        {
            get
            {
                if (_entities == null)
                    _entities = _context.Set<T>();

                return _entities;
            }
        }

        private void ThrowIfEntityIsNull(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
        }

        public virtual IQueryable<T> Table
        {
            get
            {
                return Entities;
            }
        }

        public IList<T> GetAll()
        {
            return Entities.ToList();
        }

        public IQueryable<T> Get(Expression<Func<T, bool>> predicate)
        {
            return Entities.Where(predicate);
        }

        public T Find(Expression<Func<T, bool>> predicate)
        {
            return Entities.FirstOrDefault(predicate);
        }

        public T GetById(object id)
        {
            if (typeof(T) == typeof(ApplicationUser))
            {
                var users = Entities.Include("Tokens");

                return ((IQueryable<ApplicationUser>)users).FirstOrDefault(w => w.Id == (int)id) as T;
            }

            return Entities.Find(id);
        }

        public void Insert(T entity)
        {
            try
            {
                ThrowIfEntityIsNull(entity);

                Entities.Add(entity);
            }
            catch (Exception dbEx)
            {
                throw;
            }
        }

        public async Task InsertAsync(T entity)
        {
            await new Task(() => Insert(entity));
        }

        public void Update(T entity)
        {
            try
            {
                ThrowIfEntityIsNull(entity);
                ((DataContext)_context).Entry(entity).State = EntityState.Modified;
            }
            catch (Exception dbEx)
            {
                throw;
            }
        }

        public async Task UpdateAsync(T entity)
        {
            try
            {
                await new Task(() => Update(entity));
            }
            catch (Exception dbEx)
            {
                throw;
            }
        }

        public void Delete(T entity)
        {
            try
            {
                ThrowIfEntityIsNull(entity);

                Entities.Remove(entity);
            }
            catch (Exception dbEx)
            {
                throw dbEx;
            }
        }

        public void DeleteById(int id)
        {
            try
            {
                T entity = GetById(id);

                ThrowIfEntityIsNull(entity);

                Entities.Remove(entity);
            }
            catch (Exception dbEx)
            {
                throw;
            }
        }

        public IEnumerable<S> ExecuteStoredProcedure<S>(string storedProcedure, Dictionary<string, object> parameters) where S : class
        {
            try
            {
                return _context.GetDataFromSqlCommand<S>(storedProcedure, parameters);
            }
            catch (Exception dbEx)
            {
                throw;
            }
        }

        #region Pagination

        public IQueryable<T> GetPage(int limit, int offset, string sort, bool orderByDescending)
        {
            try
            {
                var entityType = _context.Model.FindEntityType(typeof(T));

                // Table info 
                var tableName = entityType.GetTableName();
                var tableSchema = entityType.GetSchema();

                Dictionary<string, string> names = new Dictionary<string, string>();

                // Column info 
                foreach (var property in entityType.GetProperties())
                {
                    var propertyName = property.Name;
                    var columnName = property.GetColumnName(StoreObjectIdentifier.Table(tableName, tableSchema));

                    names.Add(propertyName, columnName);

                    //var columnType = property.Relational().ColumnType;
                };

                var orderByStr = "";

                if (names.Any(w => string.Compare(w.Key, sort, true) == 0))
                {
                    orderByStr = names.First(w => string.Compare(w.Key, sort, true) == 0).Value;
                }
                else
                {
                    orderByStr = "Id";
                }
                return _entities.FromSqlRaw(string.Join(" ", "SELECT * FROM", tableName, "ORDER BY", orderByStr, (orderByDescending ? "DESC" : "ASC"), "OFFSET", offset, "ROWS FETCH NEXT", limit, "ROWS ONLY"));
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            catch (Exception dbEx)
            {
                throw dbEx;
            }
        }

        #endregion


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).                  
                    _context.Dispose();
                    _entities = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Repository()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

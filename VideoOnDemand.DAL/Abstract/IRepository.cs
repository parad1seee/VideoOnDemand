using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace VideoOnDemand.DAL.Abstract
{
    public interface IRepository<T> : IDisposable where T : class
    {
        IQueryable<T> Table { get; }

        IList<T> GetAll();

        IQueryable<T> Get(Expression<Func<T, bool>> predicate);

        T Find(Expression<Func<T, bool>> predicate);

        T GetById(object id);

        void Insert(T entity);

        Task InsertAsync(T entity);

        void Update(T entity);

        Task UpdateAsync(T entity);

        void Delete(T entity);

        void DeleteById(int id);

        IEnumerable<S> ExecuteStoredProcedure<S>(string storedProcedure, Dictionary<string, object> parameters) where S : class;
    }
}

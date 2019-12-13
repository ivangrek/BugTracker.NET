/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing
{
    using System.Linq;

    public interface IRepository<TEntity, TId>
        where TEntity : class, IHaveId<TId>
    {
        void Add(TEntity entity);

        void Remove(TEntity entity);

        TEntity FindById(TId id);

        TEntity GetById(TId id);

        // simple way
        IQueryable<TEntity> GetQuery();
    }
}
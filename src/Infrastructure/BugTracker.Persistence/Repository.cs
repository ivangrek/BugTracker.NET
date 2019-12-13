/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence
{
    using System.Linq;
    using Changing;

    internal abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>
        where TEntity : class, IHaveId<TId>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public Repository(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public void Add(TEntity entity)
        {
            this.applicationDbContext
                .Set<TEntity>()
                .Add(entity);
        }

        public void Remove(TEntity entity)
        {
            this.applicationDbContext
                .Set<TEntity>()
                .Remove(entity);
        }

        public TEntity FindById(TId id)
        {
            return this.applicationDbContext
                .Set<TEntity>()
                .Find(id);
        }

        public TEntity GetById(TId id)
        {
            var entity = FindById(id);

            if (entity == null)
            {
                // add throw EntityNotFoundException
            }

            return entity;
        }

        public IQueryable<TEntity> GetQuery()
        {
            return this.applicationDbContext
                .Set<TEntity>()
                .AsNoTracking();
        }
    }
}
/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Categories
{
    using BugTracker.Tracking.Changing.Categories;

    internal sealed class CategoryRepository : Repository<Category, int>, ICategoryRepository
    {
        public CategoryRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
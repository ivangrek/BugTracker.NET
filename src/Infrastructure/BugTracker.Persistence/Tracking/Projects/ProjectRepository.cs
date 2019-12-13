/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Projects
{
    using BugTracker.Tracking.Changing.Projects;

    internal sealed class ProjectRepository : Repository<Project, int>, IProjectRepository
    {
        public ProjectRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.Projects
{
    using System.Collections.Generic;
    using BugTracker.Querying;

    public interface IProjectListResult : IResult, IEnumerable<IProjectListRow>
    {
    }

    public interface IProjectListRow
    {
        int Id { get; }

        string Name { get; }

        string Description { get; }

        string DefaultUserName { get; }

        int? AutoAssignDefaultUser { get; }

        int? AutoSubscribeDefaultUser { get; }

        int? EnablePop3 { get; }

        string Pop3Username { get; }

        string Pop3EmailFrom { get; }

        int Active { get; }

        int Default { get; }
    }
}
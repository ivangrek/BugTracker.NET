/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.Projects
{
    using BugTracker.Querying;

    public interface IProjectStateResult : IResult
    {
        int Id { get; }

        string Name { get; }

        string Description { get; }

        int? DefaultUserId { get; }

        int? AutoAssignDefaultUser { get; }

        int? AutoSubscribeDefaultUser { get; }

        int? EnablePop3 { get; }

        string Pop3Username { get; }

        string Pop3Password { get; }

        string Pop3EmailFrom { get; }

        int Active { get; }

        int Default { get; }

        int? EnableCustomDropdown1 { get; }

        string CustomDropdown1Label { get; }

        string CustomDropdown1Values { get; }

        int? EnableCustomDropdown2 { get; }

        string CustomDropdown2Label { get; }

        string CustomDropdown2Values { get; }

        int? EnableCustomDropdown3 { get; }

        string CustomDropdown3Label { get; }

        string CustomDropdown3Values { get; }
    }
}
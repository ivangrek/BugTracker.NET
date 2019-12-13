/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Projects.Commands
{
    using BugTracker.Changing;

    public interface ICreateCommand : ICommand
    {
        string Name { get; }

        string Description { get; }

        int? DefaultUserId { get; }

        bool AutoAssignDefaultUser { get; }

        bool AutoSubscribeDefaultUser { get; }

        bool EnablePop3 { get; }

        string Pop3Username { get; }

        string Pop3Password { get; }

        string Pop3EmailFrom { get; }

        bool Active { get; }

        bool Default { get; }

        bool EnableCustomDropdown1 { get; }

        string CustomDropdown1Label { get; }

        string CustomDropdown1Values { get; }

        bool EnableCustomDropdown2 { get; }

        string CustomDropdown2Label { get; }

        string CustomDropdown2Values { get; }

        bool EnableCustomDropdown3 { get; }

        string CustomDropdown3Label { get; }

        string CustomDropdown3Values { get; }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Identification.Changing.Commands
{
    using BugTracker.Changing;

    public sealed class LoginCommand : ICommand
    {
        public string Name { get; set; }

        public string Password { get; set; }
    }
}
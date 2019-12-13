/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing.Results
{
    public interface INotAuthorizedCommandResult : ICommandResult
    {
    }

    internal sealed class NotAuthorizedCommandResult : INotAuthorizedCommandResult
    {
    }
}
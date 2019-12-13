/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing.Results
{
    public interface ICommandResult
    {
    }

    public static class CommandResult
    {
        public static ICommandResult Done()
        {
            return new DoneCommandResult();
        }

        public static IFailCommandResultBuilder Fail()
        {
            return new FailCommandResultBuilder(new FailCommandResult());
        }

        public static ICommandResult NotAuthorized()
        {
            return new NotAuthorizedCommandResult();
        }
    }
}
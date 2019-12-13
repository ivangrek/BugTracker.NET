/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Identification.Changing.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand>
    {
        private readonly IUserRepository userRepository;

        public LoginCommandHandler(
            IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public void Handle(LoginCommand command, out ICommandResult commandResult)
        {
            commandResult = CommandResult.Done();
        }
    }
}
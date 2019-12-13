/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Statuses.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class DeleteCommandHandler : ICommandHandler<IDeleteCommand>
    {
        private readonly IStatusRepository statusRepository;

        public DeleteCommandHandler(
            IStatusRepository statusRepository)
        {
            this.statusRepository = statusRepository;
        }

        public void Handle(IDeleteCommand command, out ICommandResult commandResult)
        {
            var status = this.statusRepository
                .GetById(command.Id);

            this.statusRepository
                .Remove(status);

            commandResult = CommandResult.Done();
        }
    }
}
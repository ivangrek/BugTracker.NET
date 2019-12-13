/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Priorities.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class DeleteCommandHandler : ICommandHandler<IDeleteCommand>
    {
        private readonly IPriorityRepository priorityRepository;

        public DeleteCommandHandler(
            IPriorityRepository priorityRepository)
        {
            this.priorityRepository = priorityRepository;
        }

        public void Handle(IDeleteCommand command, out ICommandResult commandResult)
        {
            var priority = this.priorityRepository
                .GetById(command.Id);

            this.priorityRepository
                .Remove(priority);

            commandResult = CommandResult.Done();
        }
    }
}
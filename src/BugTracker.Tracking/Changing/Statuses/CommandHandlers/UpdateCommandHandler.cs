/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Statuses.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class UpdateCommandHandler : ICommandHandler<IUpdateCommand>
    {
        private readonly IStatusRepository statusRepository;

        public UpdateCommandHandler(
            IStatusRepository statusRepository)
        {
            this.statusRepository = statusRepository;
        }

        public void Handle(IUpdateCommand command, out ICommandResult commandResult)
        {
            var priority = this.statusRepository
                .GetById(command.Id);

            priority.Name = command.Name;
            priority.SortSequence = command.SortSequence;
            priority.Style = command.Style;
            priority.Default = command.Default ? 1 : 0;

            commandResult = CommandResult.Done();
        }
    }
}
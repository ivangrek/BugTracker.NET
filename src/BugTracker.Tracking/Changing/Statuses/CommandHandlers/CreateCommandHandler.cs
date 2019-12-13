/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Statuses.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class CreateCommandHandler : ICommandHandler<ICreateCommand>
    {
        private readonly IStatusRepository statusRepository;

        public CreateCommandHandler(
            IStatusRepository statusRepository)
        {
            this.statusRepository = statusRepository;
        }

        public void Handle(ICreateCommand command, out ICommandResult commandResult)
        {
            var status = new Status
            {
                Name = command.Name,
                SortSequence = command.SortSequence,
                Style = command.Style,
                Default = command.Default ? 1 : 0
            };

            this.statusRepository
                .Add(status);

            commandResult = CommandResult.Done();
        }
    }
}
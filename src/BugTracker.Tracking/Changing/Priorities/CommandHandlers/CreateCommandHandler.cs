/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Priorities.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class CreateCommandHandler : ICommandHandler<ICreateCommand>
    {
        private readonly IPriorityRepository priorityRepository;

        public CreateCommandHandler(
            IPriorityRepository priorityRepository)
        {
            this.priorityRepository = priorityRepository;
        }

        public void Handle(ICreateCommand command, out ICommandResult commandResult)
        {
            var priority = new Priority
            {
                Name = command.Name,
                SortSequence = command.SortSequence,
                BackgroundColor = command.BackgroundColor,
                Style = command.Style,
                Default = command.Default ? 1 : 0
            };

            this.priorityRepository
                .Add(priority);

            commandResult = CommandResult.Done();
        }
    }
}
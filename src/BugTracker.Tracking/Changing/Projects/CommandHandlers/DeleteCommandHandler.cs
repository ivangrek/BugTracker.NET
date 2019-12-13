/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Projects.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class DeleteCommandHandler : ICommandHandler<IDeleteCommand>
    {
        private readonly IProjectRepository projectRepository;

        public DeleteCommandHandler(
            IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public void Handle(IDeleteCommand command, out ICommandResult commandResult)
        {
            var project = this.projectRepository
                .GetById(command.Id);

            this.projectRepository
                .Remove(project);

            commandResult = CommandResult.Done();
        }
    }
}
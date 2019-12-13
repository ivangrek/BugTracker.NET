/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Categories.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class DeleteCommandHandler : ICommandHandler<IDeleteCommand>
    {
        private readonly ICategoryRepository categoryRepository;

        public DeleteCommandHandler(
            ICategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
        }

        public void Handle(IDeleteCommand command, out ICommandResult commandResult)
        {
            var category = this.categoryRepository
                .GetById(command.Id);

            this.categoryRepository
                .Remove(category);

            commandResult = CommandResult.Done();
        }
    }
}
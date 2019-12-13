/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Categories.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class UpdateCommandHandler : ICommandHandler<IUpdateCommand>
    {
        private readonly ICategoryRepository categoryRepository;

        public UpdateCommandHandler(
            ICategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
        }

        public void Handle(IUpdateCommand command, out ICommandResult commandResult)
        {
            var category = this.categoryRepository
                .GetById(command.Id);

            category.Name = command.Name;
            category.SortSequence = command.SortSequence;
            category.Default = command.Default ? 1 : 0;

            commandResult = CommandResult.Done();
        }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Categories.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class CreateCommandHandler : ICommandHandler<ICreateCommand>
    {
        private readonly ICategoryRepository categoryRepository;

        public CreateCommandHandler(
            ICategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
        }

        public void Handle(ICreateCommand command, out ICommandResult commandResult)
        {
            var status = new Category
            {
                Name = command.Name,
                SortSequence = command.SortSequence,
                Default = command.Default ? 1 : 0
            };

            this.categoryRepository
                .Add(status);

            commandResult = CommandResult.Done();
        }
    }
}
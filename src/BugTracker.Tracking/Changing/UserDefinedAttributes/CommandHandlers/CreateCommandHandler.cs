/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class CreateCommandHandler : ICommandHandler<ICreateCommand>
    {
        private readonly IUserDefinedAttributeRepository userDefinedAttributeRepository;

        public CreateCommandHandler(
            IUserDefinedAttributeRepository userDefinedAttributeRepository)
        {
            this.userDefinedAttributeRepository = userDefinedAttributeRepository;
        }

        public void Handle(ICreateCommand command, out ICommandResult commandResult)
        {
            var userDefinedAttribute = new UserDefinedAttribute
            {
                Name = command.Name,
                SortSequence = command.SortSequence,
                Default = command.Default ? 1 : 0
            };

            this.userDefinedAttributeRepository
                .Add(userDefinedAttribute);

            commandResult = CommandResult.Done();
        }
    }
}
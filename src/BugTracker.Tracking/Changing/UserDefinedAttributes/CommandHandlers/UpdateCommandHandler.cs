/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class UpdateCommandHandler : ICommandHandler<IUpdateCommand>
    {
        private readonly IUserDefinedAttributeRepository userDefinedAttributeRepository;

        public UpdateCommandHandler(
            IUserDefinedAttributeRepository userDefinedAttributeRepository)
        {
            this.userDefinedAttributeRepository = userDefinedAttributeRepository;
        }

        public void Handle(IUpdateCommand command, out ICommandResult commandResult)
        {
            var userDefinedAttribute = this.userDefinedAttributeRepository
                .GetById(command.Id);

            userDefinedAttribute.Name = command.Name;
            userDefinedAttribute.SortSequence = command.SortSequence;
            userDefinedAttribute.Default = command.Default ? 1 : 0;

            commandResult = CommandResult.Done();
        }
    }
}
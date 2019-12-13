/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class DeleteCommandHandler : ICommandHandler<IDeleteCommand>
    {
        private readonly IUserDefinedAttributeRepository userDefinedAttributeRepository;

        public DeleteCommandHandler(
            IUserDefinedAttributeRepository userDefinedAttributeRepository)
        {
            this.userDefinedAttributeRepository = userDefinedAttributeRepository;
        }

        public void Handle(IDeleteCommand command, out ICommandResult commandResult)
        {
            var userDefinedAttribute = this.userDefinedAttributeRepository
                .GetById(command.Id);

            this.userDefinedAttributeRepository
                .Remove(userDefinedAttribute);

            commandResult = CommandResult.Done();
        }
    }
}
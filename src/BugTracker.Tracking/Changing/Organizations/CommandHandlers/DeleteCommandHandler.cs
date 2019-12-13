/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Organizations.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class DeleteCommandHandler : ICommandHandler<IDeleteCommand>
    {
        private readonly IOrganizationRepository organizationRepository;

        public DeleteCommandHandler(
            IOrganizationRepository organizationRepository)
        {
            this.organizationRepository = organizationRepository;
        }

        public void Handle(IDeleteCommand command, out ICommandResult commandResult)
        {
            var organization = this.organizationRepository
                .GetById(command.Id);

            this.organizationRepository
                .Remove(organization);

            commandResult = CommandResult.Done();
        }
    }
}
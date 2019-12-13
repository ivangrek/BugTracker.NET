/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Projects.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class CreateCommandHandler : ICommandHandler<ICreateCommand>
    {
        private readonly IProjectRepository projectRepository;

        public CreateCommandHandler(
            IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public void Handle(ICreateCommand command, out ICommandResult commandResult)
        {
            var project = new Project
            {
                Name = command.Name,
                Description = command.Description,
                DefaultUserId = command.DefaultUserId,
                AutoAssignDefaultUser = command.AutoAssignDefaultUser ? 1 : 0,
                AutoSubscribeDefaultUser = command.AutoSubscribeDefaultUser ? 1 : 0,
                EnablePop3 = command.EnablePop3 ? 1 : 0,
                Pop3Username = command.Pop3Username,
                Pop3Password = command.Pop3Password,

                Pop3EmailFrom = command.Pop3EmailFrom,
                Active = command.Active ? 1 : 0,
                Default = command.Default ? 1 : 0,

                EnableCustomDropdown1 = command.EnableCustomDropdown1 ? 1 : 0,
                CustomDropdown1Label = command.CustomDropdown1Label,
                CustomDropdown1Values = command.CustomDropdown1Values,

                EnableCustomDropdown2 = command.EnableCustomDropdown2 ? 1 : 0,
                CustomDropdown2Label = command.CustomDropdown2Label,
                CustomDropdown2Values = command.CustomDropdown2Values,

                EnableCustomDropdown3 = command.EnableCustomDropdown3 ? 1 : 0,
                CustomDropdown3Label = command.CustomDropdown3Label,
                CustomDropdown3Values = command.CustomDropdown3Values
            };

            this.projectRepository
                .Add(project);

            commandResult = CommandResult.Done();
        }
    }
}
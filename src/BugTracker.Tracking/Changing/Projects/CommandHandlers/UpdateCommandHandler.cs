/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Projects.CommandHandlers
{
    using BugTracker.Changing;
    using BugTracker.Changing.Results;
    using Commands;

    internal sealed class UpdateCommandHandler : ICommandHandler<IUpdateCommand>
    {
        private readonly IProjectRepository projectRepository;

        public UpdateCommandHandler(
            IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public void Handle(IUpdateCommand command, out ICommandResult commandResult)
        {
            var project = this.projectRepository
                .GetById(command.Id);

            project.Name = command.Name;
            project.Description = command.Description;
            project.DefaultUserId = command.DefaultUserId;
            project.AutoAssignDefaultUser = command.AutoAssignDefaultUser ? 1 : 0;
            project.AutoSubscribeDefaultUser = command.AutoSubscribeDefaultUser ? 1 : 0;
            project.EnablePop3 = command.EnablePop3 ? 1 : 0;
            project.Pop3Username = command.Pop3Username;

            if (!string.IsNullOrEmpty(command.Pop3Password)) project.Pop3Password = command.Pop3Password;

            project.Pop3EmailFrom = command.Pop3EmailFrom;
            project.Active = command.Active ? 1 : 0;
            project.Default = command.Default ? 1 : 0;

            project.EnableCustomDropdown1 = command.EnableCustomDropdown1 ? 1 : 0;
            project.CustomDropdown1Label = command.CustomDropdown1Label;
            project.CustomDropdown1Values = command.CustomDropdown1Values;

            project.EnableCustomDropdown2 = command.EnableCustomDropdown2 ? 1 : 0;
            project.CustomDropdown2Label = command.CustomDropdown2Label;
            project.CustomDropdown2Values = command.CustomDropdown2Values;

            project.EnableCustomDropdown3 = command.EnableCustomDropdown3 ? 1 : 0;
            project.CustomDropdown3Label = command.CustomDropdown3Label;
            project.CustomDropdown3Values = command.CustomDropdown3Values;

            commandResult = CommandResult.Done();
        }
    }
}
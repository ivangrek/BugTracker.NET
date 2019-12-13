/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Projects.Validators
{
    using System.Linq;
    using Bugs;
    using Commands;
    using FluentValidation;

    internal sealed class DeleteCommandValidator : AbstractValidator<IDeleteCommand>
    {
        private readonly IBugRepository bugRepository;
        private readonly IProjectRepository projectRepository;

        public DeleteCommandValidator(
            IProjectRepository projectRepository,
            IBugRepository bugRepository)
        {
            this.projectRepository = projectRepository;
            this.bugRepository = bugRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x)
                .Must(ValidDeleting)
                .WithMessage("You can't delete project because some bugs still reference it.");
        }

        private bool Exist(IDeleteCommand command)
        {
            var project = this.projectRepository
                .FindById(command.Id);

            if (project == null) return false;

            return true;
        }

        private bool ValidDeleting(IDeleteCommand command)
        {
            var relatedBugsCount = this.bugRepository
                .GetQuery()
                .Count(x => x.ProjectId == command.Id);

            if (relatedBugsCount != 0) return false;

            return true;
        }
    }
}
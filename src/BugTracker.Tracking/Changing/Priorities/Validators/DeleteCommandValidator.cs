/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Priorities.Validators
{
    using System.Linq;
    using Bugs;
    using Commands;
    using FluentValidation;

    internal sealed class DeleteCommandValidator : AbstractValidator<IDeleteCommand>
    {
        private readonly IBugRepository bugRepository;
        private readonly IPriorityRepository priorityRepository;

        public DeleteCommandValidator(
            IPriorityRepository priorityRepository,
            IBugRepository bugRepository)
        {
            this.priorityRepository = priorityRepository;
            this.bugRepository = bugRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x)
                .Must(ValidDeleting)
                .WithMessage("You can't delete priority because some bugs still reference it.");
        }

        private bool Exist(IDeleteCommand command)
        {
            var priority = this.priorityRepository
                .FindById(command.Id);

            if (priority == null) return false;

            return true;
        }

        private bool ValidDeleting(IDeleteCommand command)
        {
            var relatedBugsCount = this.bugRepository
                .GetQuery()
                .Count(x => x.PriorityId == command.Id);

            if (relatedBugsCount != 0) return false;

            return true;
        }
    }
}
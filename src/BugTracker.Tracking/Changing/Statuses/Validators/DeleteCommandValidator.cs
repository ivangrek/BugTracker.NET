/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Statuses.Validators
{
    using System.Linq;
    using Bugs;
    using Commands;
    using FluentValidation;

    internal sealed class DeleteCommandValidator : AbstractValidator<IDeleteCommand>
    {
        private readonly IBugRepository bugRepository;
        private readonly IStatusRepository statusRepository;

        public DeleteCommandValidator(
            IStatusRepository statusRepository,
            IBugRepository bugRepository)
        {
            this.statusRepository = statusRepository;
            this.bugRepository = bugRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x)
                .Must(ValidDeleting)
                .WithMessage("You can't delete status because some bugs still reference it.");
        }

        private bool Exist(IDeleteCommand command)
        {
            var status = this.statusRepository
                .FindById(command.Id);

            if (status == null) return false;

            return true;
        }

        private bool ValidDeleting(IDeleteCommand command)
        {
            var relatedBugsCount = this.bugRepository
                .GetQuery()
                .Count(x => x.StatusId == command.Id);

            if (relatedBugsCount != 0) return false;

            return true;
        }
    }
}
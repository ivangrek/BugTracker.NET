/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Categories.Validators
{
    using System.Linq;
    using Bugs;
    using Commands;
    using FluentValidation;

    internal sealed class DeleteCommandValidator : AbstractValidator<IDeleteCommand>
    {
        private readonly IBugRepository bugRepository;
        private readonly ICategoryRepository categoryRepository;

        public DeleteCommandValidator(
            ICategoryRepository categoryRepository,
            IBugRepository bugRepository)
        {
            this.categoryRepository = categoryRepository;
            this.bugRepository = bugRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x)
                .Must(ValidDeleting)
                .WithMessage("You can't delete category because some bugs still reference it.");
        }

        private bool Exist(IDeleteCommand command)
        {
            var category = this.categoryRepository
                .FindById(command.Id);

            if (category == null) return false;

            return true;
        }

        private bool ValidDeleting(IDeleteCommand command)
        {
            var relatedBugsCount = this.bugRepository
                .GetQuery()
                .Count(x => x.CategoryId == command.Id);

            if (relatedBugsCount != 0) return false;

            return true;
        }
    }
}
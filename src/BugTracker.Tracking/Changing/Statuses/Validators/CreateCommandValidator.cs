/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Statuses.Validators
{
    using System.Linq;
    using Commands;
    using FluentValidation;

    internal sealed class CreateCommandValidator : AbstractValidator<ICreateCommand>
    {
        private readonly IStatusRepository statusRepository;

        public CreateCommandValidator(
            IStatusRepository statusRepository)
        {
            this.statusRepository = statusRepository;

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .Must(UniqueName)
                .WithMessage("Status with same name exist.");

            RuleFor(x => x.SortSequence)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sort Sequence must be greater than or equal to 0.");
        }

        private bool UniqueName(string name)
        {
            var status = this.statusRepository
                .GetQuery()
                .FirstOrDefault(x => x.Name == name);

            if (status != null) return false;

            return true;
        }
    }
}
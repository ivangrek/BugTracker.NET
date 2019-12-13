/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Statuses.Validators
{
    using System.Linq;
    using Commands;
    using FluentValidation;

    internal sealed class UpdateCommandValidator : AbstractValidator<IUpdateCommand>
    {
        private readonly IStatusRepository statusRepository;

        public UpdateCommandValidator(
            IStatusRepository statusRepository)
        {
            this.statusRepository = statusRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .Must(UniqueName)
                .WithMessage("Status with same name exist.");

            RuleFor(x => x.SortSequence)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sort Sequence must be greater than or equal to 0.");
        }

        private bool Exist(IUpdateCommand command)
        {
            var status = this.statusRepository
                .FindById(command.Id);

            if (status == null) return false;

            return true;
        }

        private bool UniqueName(IUpdateCommand command, string name)
        {
            var status = this.statusRepository
                .GetQuery()
                .Where(x => x.Id != command.Id)
                .FirstOrDefault(x => x.Name == name);

            if (status != null) return false;

            return true;
        }
    }
}
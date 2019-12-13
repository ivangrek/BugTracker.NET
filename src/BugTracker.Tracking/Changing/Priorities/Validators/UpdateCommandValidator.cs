/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Priorities.Validators
{
    using System.Linq;
    using Commands;
    using FluentValidation;

    internal sealed class UpdateCommandValidator : AbstractValidator<IUpdateCommand>
    {
        private readonly IPriorityRepository priorityRepository;

        public UpdateCommandValidator(
            IPriorityRepository priorityRepository)
        {
            this.priorityRepository = priorityRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .Must(UniqueName)
                .WithMessage("Priority with same name exist.");

            RuleFor(x => x.SortSequence)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sort Sequence must be greater than or equal to 0.");

            RuleFor(x => x.BackgroundColor)
                .NotEmpty()
                .WithMessage("Background Color in #FFFFFF format is required.");
        }

        private bool Exist(IUpdateCommand command)
        {
            var priority = this.priorityRepository
                .FindById(command.Id);

            if (priority == null) return false;

            return true;
        }

        private bool UniqueName(IUpdateCommand command, string name)
        {
            var priority = this.priorityRepository
                .GetQuery()
                .Where(x => x.Id != command.Id)
                .FirstOrDefault(x => x.Name == name);

            if (priority != null) return false;

            return true;
        }
    }
}
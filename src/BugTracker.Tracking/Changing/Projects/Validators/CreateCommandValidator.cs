/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Projects.Validators
{
    using System.Linq;
    using Commands;
    using FluentValidation;

    internal sealed class CreateCommandValidator : AbstractValidator<ICreateCommand>
    {
        private readonly IProjectRepository projectRepository;

        public CreateCommandValidator(
            IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .Must(UniqueName)
                .WithMessage("Project with same name exist.");

            RuleFor(x => x.CustomDropdown1Values)
                .Must(ValidDropdownValues)
                .WithMessage("Special characters like <, >, or quotes not allowed.");

            RuleFor(x => x.CustomDropdown2Values)
                .Must(ValidDropdownValues)
                .WithMessage("Special characters like <, >, or quotes not allowed.");

            RuleFor(x => x.CustomDropdown3Values)
                .Must(ValidDropdownValues)
                .WithMessage("Special characters like <, >, or quotes not allowed.");
        }

        private bool UniqueName(string name)
        {
            var status = this.projectRepository
                .GetQuery()
                .FirstOrDefault(x => x.Name == name);

            if (status != null) return false;

            return true;
        }

        private bool ValidDropdownValues(ICreateCommand command, string values)
        {
            if (string.IsNullOrEmpty(values)) return true;

            if (values.Contains("'")
                || values.Contains("\"")
                || values.Contains("<")
                || values.Contains(">")
                || values.Contains("\t"))
                return false;

            return true;
        }
    }
}
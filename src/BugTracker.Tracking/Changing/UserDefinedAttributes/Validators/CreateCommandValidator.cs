/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes.Validators
{
    using System.Linq;
    using Commands;
    using FluentValidation;

    internal sealed class CreateCommandValidator : AbstractValidator<ICreateCommand>
    {
        private readonly IUserDefinedAttributeRepository userDefinedAttributeRepository;

        public CreateCommandValidator(
            IUserDefinedAttributeRepository userDefinedAttributeRepository)
        {
            this.userDefinedAttributeRepository = userDefinedAttributeRepository;

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .Must(UniqueName)
                .WithMessage("Attribute with same name exist.");

            RuleFor(x => x.SortSequence)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sort Sequence must be greater than or equal to 0.");
        }

        private bool UniqueName(string name)
        {
            var status = this.userDefinedAttributeRepository
                .GetQuery()
                .FirstOrDefault(x => x.Name == name);

            if (status != null) return false;

            return true;
        }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes.Validators
{
    using System.Linq;
    using Commands;
    using FluentValidation;

    internal sealed class UpdateCommandValidator : AbstractValidator<IUpdateCommand>
    {
        private readonly IUserDefinedAttributeRepository userDefinedAttributeRepository;

        public UpdateCommandValidator(
            IUserDefinedAttributeRepository userDefinedAttributeRepository)
        {
            this.userDefinedAttributeRepository = userDefinedAttributeRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .Must(UniqueName)
                .WithMessage("Attribute with same name exist.");

            RuleFor(x => x.SortSequence)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sort Sequence must be greater than or equal to 0.");
        }

        private bool Exist(IUpdateCommand command)
        {
            var status = this.userDefinedAttributeRepository
                .FindById(command.Id);

            if (status == null) return false;

            return true;
        }

        private bool UniqueName(IUpdateCommand command, string name)
        {
            var status = this.userDefinedAttributeRepository
                .GetQuery()
                .Where(x => x.Id != command.Id)
                .FirstOrDefault(x => x.Name == name);

            if (status != null) return false;

            return true;
        }
    }
}
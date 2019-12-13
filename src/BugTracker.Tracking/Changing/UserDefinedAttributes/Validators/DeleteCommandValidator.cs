/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes.Validators
{
    using System.Linq;
    using Bugs;
    using Commands;
    using FluentValidation;

    internal sealed class DeleteCommandValidator : AbstractValidator<IDeleteCommand>
    {
        private readonly IBugRepository bugRepository;
        private readonly IUserDefinedAttributeRepository userDefinedAttributeRepository;

        public DeleteCommandValidator(
            IUserDefinedAttributeRepository userDefinedAttributeRepository,
            IBugRepository bugRepository)
        {
            this.userDefinedAttributeRepository = userDefinedAttributeRepository;
            this.bugRepository = bugRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x)
                .Must(ValidDeleting)
                .WithMessage("You can't delete attribute because some bugs still reference it.");
        }

        private bool Exist(IDeleteCommand command)
        {
            var userDefinedAttribute = this.userDefinedAttributeRepository
                .FindById(command.Id);

            if (userDefinedAttribute == null) return false;

            return true;
        }

        private bool ValidDeleting(IDeleteCommand command)
        {
            var relatedBugsCount = this.bugRepository
                .GetQuery()
                .Count(x => x.UserDefinedAttributeId == command.Id);

            if (relatedBugsCount != 0) return false;

            return true;
        }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Organizations.Validators
{
    using System.Linq;
    using Bugs;
    using Commands;
    using FluentValidation;

    internal sealed class DeleteCommandValidator : AbstractValidator<IDeleteCommand>
    {
        private readonly IBugRepository bugRepository;
        private readonly IOrganizationRepository organizationRepository;

        public DeleteCommandValidator(
            IOrganizationRepository organizationRepository,
            IBugRepository bugRepository)
        {
            this.organizationRepository = organizationRepository;
            this.bugRepository = bugRepository;

            RuleFor(x => x)
                .Must(Exist)
                .WithMessage("Not found.");

            RuleFor(x => x)
                .Must(ValidDeleting)
                .WithMessage("You can't delete organization because some bugs still reference it.");
        }

        private bool Exist(IDeleteCommand command)
        {
            var organization = this.organizationRepository
                .FindById(command.Id);

            if (organization == null) return false;

            return true;
        }

        private bool ValidDeleting(IDeleteCommand command)
        {
            var relatedBugsCount = this.bugRepository
                .GetQuery()
                .Count(x => x.OrganizationId == command.Id);

            if (relatedBugsCount != 0) return false;

            return true;

            //var sql = @"declare @cnt int
            //    select @cnt = count(1) from users where us_org = $1;
            //    select @cnt = @cnt + count(1) from queries where qu_org = $1;
            //    select @cnt = @cnt + count(1) from bugs where bg_org = $1;
            //    select og_name, @cnt [cnt] from orgs where og_id = $1"
            //    .Replace("$1", id.ToString());

            //var dr = DbUtil.GetDataRow(sql);

            //if ((int)dr["cnt"] > 0)
            //{
            //    return Content($"You can't delete organization \"{dr["og_name"]}\" because some bugs still reference it.");
            //}
        }
    }
}
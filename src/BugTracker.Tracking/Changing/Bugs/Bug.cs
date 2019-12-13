/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Bugs
{
    using BugTracker.Changing;

    public sealed class Bug : IHaveId<int>
    {
        public int OrganizationId { get; set; }

        public int CategoryId { get; set; }

        public int ProjectId { get; set; }

        public int PriorityId { get; set; }

        public int StatusId { get; set; }

        public int UserDefinedAttributeId { get; set; }
        public int Id { get; set; }
    }
}
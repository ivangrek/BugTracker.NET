/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Attachment
{
    public sealed class UpdateModel
    {
        public int Id { get; set; }

        public int BugId { get; set; }

        public string Description { get; set; }

        public bool InternalOnly { get; set; }
    }
}
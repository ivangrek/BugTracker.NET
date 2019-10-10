/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Attachment
{
    using System.Web;

    public sealed class CreateModel
    {
        public int BugId { get; set; }

        public string Description { get; set; }

        public HttpPostedFileBase File { get; set; }

        public bool InternalOnly { get; set; }
    }
}
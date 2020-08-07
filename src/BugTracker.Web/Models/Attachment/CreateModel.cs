/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Attachment
{
    using System.ComponentModel.DataAnnotations;
    using System.Web;

    public sealed class CreateModel
    {
        public int BugId { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "File")]
        public HttpPostedFileBase File { get; set; }

        [Display(Name = "Visible to internal users only")]
        public bool InternalOnly { get; set; }
    }
}
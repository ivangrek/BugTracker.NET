/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Attachment
{
    using System.ComponentModel.DataAnnotations;

    public sealed class UpdateModel
    {
        public int Id { get; set; }

        public int BugId { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Visible to internal users only")]
        public bool InternalOnly { get; set; }
    }
}
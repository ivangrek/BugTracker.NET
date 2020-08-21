/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    using System.ComponentModel.DataAnnotations;

    public sealed class RelationshipModel
    {
        public int BugId { get; set; }

        [Display(Name = "Related ID")]
        [Required(ErrorMessage = "Related ID is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Related ID must be an integer.")]
        public int RelatedBugId { get; set; }

        [Display(Name = "Comment")]
        public string Comment { get; set; }

        [Display(Name = "Related ID is")]
        public int Relation { get; set; }

        public string Action { get; set; }
    }
}
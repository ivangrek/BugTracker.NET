/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    using System.ComponentModel.DataAnnotations;

    public sealed class MergeModel
    {
        public int Id { get; set; }

        [Display(Name = "FROM bug")]
        [Required(ErrorMessage = "\"From\" bug is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "\"From\" bug must be an integer.")]
        public int FromBugId { get; set; }

        [Display(Name = "INTO bug")]
        [Required(ErrorMessage = "\"Into\" bug is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "\"Into\" bug must be an integer.")]
        public int IntoBugId { get; set; }

        public bool Confirm { get; set; }
    }
}
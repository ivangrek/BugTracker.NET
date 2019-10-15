/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.CustomField
{
    using System.ComponentModel.DataAnnotations;

    public sealed class UpdateModel
    {
        public int Id { get; set; }

        [Display(Name = "Field Name")]
        public string Name { get; set; }

        [Display(Name = "Sort Sequence")]
        [Required(ErrorMessage = "Sort Sequence is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Sort Sequence must be an integer.")]
        public int SortSequence { get; set; }

        public string DropdownType { get; set; }

        [Display(Name = "Default")]
        public string Default { get; set; }

        public string DefaultName { get; set; }

        public string DefaultValue { get; set; }

        [Display(Name = "Normal Dropdown Values")]
        public string DropdownValues { get; set; }
    }
}
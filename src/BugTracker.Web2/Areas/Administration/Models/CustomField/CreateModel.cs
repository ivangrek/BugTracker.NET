/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.CustomField
{
    using System.ComponentModel.DataAnnotations;

    public sealed class CreateModel
    {
        [Display(Name = "Field Name")]
        [Required(ErrorMessage = "Field name is required.")]
        public string Name { get; set; }

        [Display(Name = "Sort Sequence")]
        [Required(ErrorMessage = "Sort Sequence is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Sort Sequence must be an integer.")]
        public int SortSequence { get; set; }

        [Display(Name = "Dropdown Type")]
        public string DropdownType { get; set; }

        [Display(Name = "Datatype")]
        public string DataType { get; set; }

        [Display(Name = "Length/Precision")]
        public int Length { get; set; }

        [Display(Name = "Required (NULL or NOT NULL)")]
        public bool Required { get; set; }

        [Display(Name = "Default")]
        public string Default { get; set; }

        [Display(Name = "Normal Dropdown Values")]
        public string DropdownValues { get; set; }
    }
}
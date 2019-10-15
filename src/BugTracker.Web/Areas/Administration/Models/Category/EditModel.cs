﻿/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Category
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Display(Name = "Sort Sequence")]
        [Required(ErrorMessage = "Sort Sequence is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Sort Sequence must be an integer.")]
        public int SortSequence { get; set; }

        [Display(Name = "Default Selection")]
        public bool Default { get; set; }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Priority
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Sort Sequence is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Sort Sequence must be an integer.")]
        public int SortSequence { get; set; }

        [Required(ErrorMessage = "Background Color in #FFFFFF format is required.")]
        public string BackgroundColor { get; set; }

        public string Style { get; set; }

        public bool Default { get; set; }
    }
}
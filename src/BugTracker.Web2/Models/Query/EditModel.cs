/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Query
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Display(Name = "Visibility")]
        [Required(ErrorMessage = "Chart Type is required.")]
        public int Visibility { get; set; }

        public int UserId { get; set; }

        public int QrganizationId { get; set; }

        [Display(Name = "SQL")]
        [Required(ErrorMessage = "The SQL statement is required.")]
        public string SqlText { get; set; }
    }
}
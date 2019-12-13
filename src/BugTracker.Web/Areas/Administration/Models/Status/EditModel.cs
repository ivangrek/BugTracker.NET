/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Status
{
    using System.ComponentModel.DataAnnotations;
    using Tracking.Changing.Statuses.Commands;

    public sealed class EditModel : ICreateCommand, IUpdateCommand
    {
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Sort Sequence")]
        public int SortSequence { get; set; }

        [Display(Name = "CSS Class")]
        public string Style { get; set; }

        [Display(Name = "Default Selection")]
        public bool Default { get; set; }

        public int Id { get; set; }
    }
}
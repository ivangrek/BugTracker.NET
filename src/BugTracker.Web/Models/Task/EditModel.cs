/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Task
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Display(Name = "Bug ID")]
        public int BugId { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Description is required.")]
        public string Name { get; set; }

        [Display(Name = "Assigned to")]
        public int UserId { get; set; }

        [Display(Name = "Planned start date")]
        public string PlannedStartDate { get; set; }

        [Display(Name = "hour")]
        public string PlannedStartHour { get; set; }

        [Display(Name = "min")]
        public string PlannedStartMinute { get; set; }

        [Display(Name = "Planned end date")]
        public string PlannedEndDate { get; set; }

        [Display(Name = "hour")]
        public string PlannedEndHour { get; set; }

        [Display(Name = "min")]
        public string PlannedEndMinute { get; set; }

        [Display(Name = "Actual start date")]
        public string ActualStartDate { get; set; }

        [Display(Name = "hour")]
        public string ActualStartHour { get; set; }

        [Display(Name = "min")]
        public string ActualStartMinute { get; set; }

        [Display(Name = "Actual end date")]
        public string ActualEndDate { get; set; }

        [Display(Name = "hour")]
        public string ActualEndHour { get; set; }

        [Display(Name = "min")]
        public string ActualEndMinute { get; set; }

        [Display(Name = "Planned duration")]
        public string PlannedDuration { get; set; }

        [Display(Name = "Actual duration")]
        public string ActualDuration { get; set; }

        [Display(Name = "Duration units")]
        public string DurationUnitId { get; set; }

        [Display(Name = "Percent complete")]
        public string PercentComplete { get; set; }

        [Display(Name = "Status")]
        public int StatusId { get; set; }

        [Display(Name = "Sort Sequence")]
        [Required(ErrorMessage = "Sort Sequence is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Sort Sequence must be an integer.")]
        public int SortSequence { get; set; }
    }
}
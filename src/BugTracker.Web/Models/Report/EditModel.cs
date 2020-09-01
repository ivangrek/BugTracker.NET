namespace BugTracker.Web.Models.Report
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Description is required.")]
        public string Name { get; set; }

        [Display(Name = "Chart Type")]
        [Required(ErrorMessage = "Chart Type is required.")]
        public string ChartType { get; set; }

        [Display(Name = "SQL")]
        [Required(ErrorMessage = "The SQL statement is required.")]
        public string SqlText { get; set; }
    }
}
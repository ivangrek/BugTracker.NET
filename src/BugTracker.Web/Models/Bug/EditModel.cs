/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        [Display(Name = "Bug ID")]
        public int Id { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Description is required.")]
        public string Name { get; set; }

        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Display(Name = "Organization")]
        public int OrganizationId { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Priority")]
        public int PriorityId { get; set; }

        [Display(Name = "Status")]
        public int StatusId { get; set; }

        [Display(Name = "User Defined Attribute")]
        public int UserDefinedAttributeId { get; set; }

        [Display(Name = "Assigned to")]
        public int UserId { get; set; }

        [Display(Name = "Comment")]
        public string Comment { get; set; }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Web.Mvc;

    public sealed class EditModel
    {
        public EditModel()
        {
            CustomFieldValues = new Dictionary<string, string>();
        }

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
        [AllowHtml]
        public string Comment { get; set; }

        public Dictionary<string, string> CustomFieldValues { get; set; }

        public string ProjectCustomFieldValue1 { get; set; }

        public string ProjectCustomFieldValue2 { get; set; }

        public string ProjectCustomFieldValue3 { get; set; }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Search
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Web.Mvc;

    public sealed class IndexModel
    {
        public IndexModel()
        {
            ReportedByUserIds = new List<int>();
            CategoryIds = new List<int>();
            PriorityIds = new List<int>();
            AssignedToUserIds = new List<int>();
            StatusIds = new List<int>();
            OrganizationIds = new List<int>();
            ProjectIds = new List<int>();
            UdfIds = new List<int>();
            ProjectCustomValues1 = new List<string>();
            ProjectCustomValues2 = new List<string>();
            ProjectCustomValues3 = new List<string>();
        }

        [Display(Name = "Reported by")]
        public List<int> ReportedByUserIds { get; set; }

        [Display(Name = "Category")]
        public List<int> CategoryIds { get; set; }

        [Display(Name = "Priority")]
        public List<int> PriorityIds { get; set; }

        [Display(Name = "Assigned to")]
        public List<int> AssignedToUserIds { get; set; }

        [Display(Name = "Status")]
        public List<int> StatusIds { get; set; }

        [Display(Name = "Organization")]
        public List<int> OrganizationIds { get; set; }

        [Display(Name = "Project")]
        public List<int> ProjectIds { get; set; }

        [Display(Name = "Udf")]
        public List<int> UdfIds { get; set; }

        public List<string> ProjectCustomValues1 { get; set; }

        public List<string> ProjectCustomValues2 { get; set; }

        public List<string> ProjectCustomValues3 { get; set; }

        [Display(Name = "Bug description contains")]
        public string DescriptionContains { get; set; }

        [Display(Name = "Bug comments contain")]
        public string CommentContains { get; set; }

        [Display(Name = "Bug comments since")]
        public string CommentSince { get; set; }

        [Display(Name = "\"Reported on\" from date")]
        public string ReportedOnFrom { get; set; }

        [Display(Name = "to")]
        public string ReportedOnTo { get; set; }

        [Display(Name = "\"Last updated on\" from date")]
        public string LastupdatedOnFrom { get; set; }

        [Display(Name = "to")]
        public string LastupdatedOnTo { get; set; }

        [Display(Name = "Use logic")]
        public bool UseOrLogic { get; set; }

        #region Grid

        public string Action { get; set; }

        public int NewPage { get; set; }

        public string Filter { get; set; }

        public int Sort { get; set; }

        public int PrevSort { get; set; }

        public string PrevDir { get; set; }

        public string Tags { get; set; }

        [AllowHtml]
        public string SqlQuery { get; set; }

        #endregion Grid
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Web;
    using System.Web.Mvc;

    public sealed class SendEmailModel
    {
        public SendEmailModel()
        {
            Attachments = Array.Empty<string>();
        }

        public int BugId { get; set; }

        public string BugDescription { get; set; }

        [Display(Name = "To")]
        [AllowHtml]
        public string To { get; set; }

        [Display(Name = "From")]
        public string From { get; set; }

        [Display(Name = "CC")]
        [AllowHtml]
        public string CC { get; set; }

        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Display(Name = "Attachment")]
        public HttpPostedFileBase Attachment { get; set; }

        [Display(Name = "Select attachments to forward")]
        public string[] Attachments { get; set; }

        [Display(Name = "Priority")]
        public string Priority { get; set; }

        [Display(Name = "Return receipt")]
        public bool ReturnReceipt { get; set; }

        [Display(Name = "Include print of bug")]
        public bool IncludePrintOfBug { get; set; }

        [Display(Name = "Include comments visible to internal users only")]
        public bool IncludeCommentsVisibleToInternalUsersOnly { get; set; }

        [Display(Name = "Body")]
        [AllowHtml]
        public string Body { get; set; }
    }
}
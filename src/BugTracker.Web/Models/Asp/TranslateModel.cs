/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Asp
{
    using System.ComponentModel.DataAnnotations;

    public sealed class TranslateModel
    {
        public int BugId { get; set; }

        [Display(Name = "Translation mode")]
        public string TranslationMode { get; set; }

        [Display(Name = "Source text")]
        public string Source { get; set; }
    }
}
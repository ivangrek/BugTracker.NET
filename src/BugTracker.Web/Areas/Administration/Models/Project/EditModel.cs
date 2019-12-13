/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Project
{
    using System.ComponentModel.DataAnnotations;
    using Tracking.Changing.Projects.Commands;

    public sealed class EditModel : ICreateCommand, IUpdateCommand
    {
        [Display(Name = "Project Name")]
        public string Name { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; }

        [Display(Name = "Default Selection in \"projects\" Dropdown")]
        public bool Default { get; set; }

        [Display(Name = "Default User")]
        public int? DefaultUserId { get; set; }

        [Display(Name = "Auto-Assign New bugs to Default User")]
        public bool AutoAssignDefaultUser { get; set; }

        [Display(Name = "Auto-Subscribe Default User to Notifications")]
        public bool AutoSubscribeDefaultUser { get; set; }

        [Display(Name = "Enable Receiving bugs via POP3 (btnet_service.exe)")]
        public bool EnablePop3 { get; set; }

        [Display(Name = "Pop3 Username")]
        public string Pop3Username { get; set; }

        [Display(Name = "Pop3 Password")]
        public string Pop3Password { get; set; }

        [Display(Name = "From Email Address")]
        public string Pop3EmailFrom { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Enable Custom Dropdown 1")]
        public bool EnableCustomDropdown1 { get; set; }

        [Display(Name = "Custom Dropdown Label 1")]
        public string CustomDropdown1Label { get; set; }

        [Display(Name = "Custom Dropdown Values 1")]
        public string CustomDropdown1Values { get; set; }

        [Display(Name = "Enable Custom Dropdown 2")]
        public bool EnableCustomDropdown2 { get; set; }

        [Display(Name = "Custom Dropdown Label 2")]
        public string CustomDropdown2Label { get; set; }

        [Display(Name = "Custom Dropdown Values 2")]
        public string CustomDropdown2Values { get; set; }

        [Display(Name = "Enable Custom Dropdown 3")]
        public bool EnableCustomDropdown3 { get; set; }

        [Display(Name = "Custom Dropdown Label 3")]
        public string CustomDropdown3Label { get; set; }

        [Display(Name = "Custom Dropdown Values 3")]
        public string CustomDropdown3Values { get; set; }

        public int Id { get; set; }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Project
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Name { get; set; }

        public string Domain { get; set; }

        public bool Active { get; set; }

        public bool Default { get; set; }
        
        public int DefaultUser { get; set; }

        public bool AutoAssign { get; set; }

        public bool AutoSubscribe { get; set; }
        
        
        
        
        public bool EnablePop3 { get; set; }
        
        public string Pop3Login { get; set; }

        public string Pop3Password { get; set; }

        public string Pop3Email { get; set; }



        public string Description { get; set; }
        
        
        
        public bool EnableCustomDropdown1 { get; set; }

        public string CustomDropdown1Label { get; set; }

        public string CustomDropdown1Values { get; set; }

        public bool EnableCustomDropdown2 { get; set; }

        public string CustomDropdown2Label { get; set; }

        public string CustomDropdown2Values { get; set; }

        public bool EnableCustomDropdown3 { get; set; }

        public string CustomDropdown3Label { get; set; }

        public string CustomDropdown3Values { get; set; }
    }
}
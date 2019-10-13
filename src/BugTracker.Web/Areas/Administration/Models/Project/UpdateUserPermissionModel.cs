/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Project
{
    using System.Collections.Generic;

    public sealed class UpdateUserPermissionModel
    {
        public int Id { get; set; }
        
        public bool ToProjects { get; set; }

        public Dictionary<string, string[]> Permission { get; set; }
    }
}
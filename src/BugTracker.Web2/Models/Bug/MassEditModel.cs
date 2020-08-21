/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    public sealed class MassEditModel
    {
        public string Action { get; set; }

        public string BugList { get; set; }

        public string ButtonText { get; set; }

        public string Sql { get; set; }
    }
}
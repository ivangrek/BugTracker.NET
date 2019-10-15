/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.User
{
    public sealed class IndexModel
    {
        public string Filter { get; set; }

        public bool HideInactive { get; set; }
    }
}
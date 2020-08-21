/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    public sealed class IndexModel
    {
        public IndexModel()
        {
            Action = string.Empty;
            Filter = string.Empty;
        }

        public int QueryId { get; set; }

        public string Action { get; set; }

        public int NewPage { get; set; }

        public string Filter { get; set; }

        public int Sort { get; set; }

        public int PrevSort { get; set; }

        public string PrevDir { get; set; }

        public string Tags { get; set; }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Task
{
    public sealed class DeleteModel
    {
        public int Id { get; set; }

        public int BugId { get; set; }

        public string Name { get; set; }
    }
}
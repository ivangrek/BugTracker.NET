/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Bug
{
    public sealed class CreateSubscriberModel
    {
        public int Id { get; set; }

        public int UserId { get; set; }
    }
}
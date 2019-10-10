/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Account
{
    public sealed class ForgotModel
    {
        public string Login { get; set; }

        public string Email { get; set; }
    }
}
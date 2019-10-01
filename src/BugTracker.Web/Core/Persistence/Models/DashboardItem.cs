/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Models
{
    public class DashboardItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ReportId { get; set; }

        public string ChartType { get; set; }

        public int Column { get; set; }

        public int Row { get; set; }
    }
}
/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Models
{
    public class Report
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Sql { get; set; }

        public string ChartType { get; set; }
    }
}
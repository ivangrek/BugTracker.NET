/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Models
{
    public class UserDefinedAttribute
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int SortSequence { get; set; }

        public int Default { get; set; }
    }
}
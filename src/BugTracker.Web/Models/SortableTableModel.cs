/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models
{
    using System.Data;

    public sealed class SortableTableModel
    {
        public DataSet DataSet { set; get; }

        public string EditUrl { set; get; } = string.Empty;

        public string DeleteUrl { set; get; } = string.Empty;

        public bool HtmlEncode { set; get; } = true;

        public bool WriteColumnHeadingsAsLinks { set; get; } = true;
    }
}
﻿/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Home
{
    using System.Web.Mvc;

    public sealed class EditCustomHtmlModel
    {
        public string Which { get; set; }

        [AllowHtml]
        public string Text { get; set; }
    }
}
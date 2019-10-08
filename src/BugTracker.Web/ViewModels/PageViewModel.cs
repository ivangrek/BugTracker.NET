﻿/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.ViewModels
{
    using BugTracker.Web.Core;

    public class PageViewModel
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public ISecurity Security { get; set; }

        public string Title { get; set; }

        public string SelectedItem { get; set; }
    }
}
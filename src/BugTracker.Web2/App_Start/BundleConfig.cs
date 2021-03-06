﻿/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System.Web.Optimization;

    internal static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/bundles/css/app")
                .Include("~/Content/btnet.css"));

            bundles.Add(new ScriptBundle("~/bundles/js/app"));
        }
    }
}
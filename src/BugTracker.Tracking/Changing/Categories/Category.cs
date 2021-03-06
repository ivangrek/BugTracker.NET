﻿/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Categories
{
    using BugTracker.Changing;

    public sealed class Category : IHaveId<int>
    {
        public string Name { get; set; }

        public int SortSequence { get; set; }

        public int Default { get; set; }
        public int Id { get; set; }
    }
}
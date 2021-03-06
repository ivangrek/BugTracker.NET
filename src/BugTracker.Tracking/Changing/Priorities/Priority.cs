﻿/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Priorities
{
    using BugTracker.Changing;

    public sealed class Priority : IHaveId<int>
    {
        public string Name { get; set; }

        public string BackgroundColor { get; set; }

        public int SortSequence { get; set; }

        public string Style { get; set; }

        public int Default { get; set; }
        public int Id { get; set; }
    }
}
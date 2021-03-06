﻿/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Bugs
{
    using BugTracker.Changing;

    public interface IBugRepository : IRepository<Bug, int>
    {
    }
}
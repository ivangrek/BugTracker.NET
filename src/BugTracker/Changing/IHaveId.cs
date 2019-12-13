/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing
{
    public interface IHaveId<out TId>
    {
        TId Id { get; }
    }
}
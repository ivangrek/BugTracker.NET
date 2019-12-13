/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying.Results
{
    public interface IIdName
    {
        int Id { get; }

        string Name { get; }
    }
}
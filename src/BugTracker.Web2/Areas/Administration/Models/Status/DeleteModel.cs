﻿/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Status
{
    using System.ComponentModel.DataAnnotations;
    using Tracking.Changing.Statuses.Commands;

    public sealed class DeleteModel : IDeleteCommand
    {
        [Display(Name = "Name")]
        public string Name { get; set; }

        public int Id { get; set; }
    }
}
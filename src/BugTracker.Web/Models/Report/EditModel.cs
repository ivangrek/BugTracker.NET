﻿/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Report
{
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Chart Type is required.")]
        public string ChartType { get; set; }

        [Required(ErrorMessage = "The SQL statement is required.")]
        public string SqlText { get; set; }
    }
}
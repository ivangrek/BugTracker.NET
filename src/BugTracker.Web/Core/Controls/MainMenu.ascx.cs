namespace BugTracker.Web.Core.Controls
{
    using System;
    using System.Web.UI;

    public static class MainMenuSections
    {
        public const string Administration = "admin";
        public const string Reports = "reports";
        public const string Queries = "queries";
        public const string Settings = "settings";
        public const string Search = "search";
        public const string News = "news";
    }

    public partial class MainMenu : UserControl
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public string SelectedItem { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
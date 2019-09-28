namespace BugTracker.Web.Core.Controls
{
    using System;
    using System.Web.UI;

    public partial class MainMenu : UserControl
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public Security Security { get; set; }

        public string SelectedItem { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
namespace btnet
{
    using System;
    using System.Configuration;
    using System.Windows.Forms;

    public partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();

            this.textBoxUrl.Text = Program.url;
            this.textBoxUsername.Text = Program.username;
            this.textBoxPassword.Text = Program.password;
            this.textBoxDomain.Text = Program.domain;
            this.checkBoxSavePassword.Checked = Program.save_password == "1";
            this.textBoxProjectNumber.Text = Convert.ToString(Program.project_id);
        }

        public static void WriteConfig()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings.Clear();

            config.AppSettings.Settings.Add("main_window_width", Convert.ToString(Program.main_window_width));
            config.AppSettings.Settings.Add("main_window_height", Convert.ToString(Program.main_window_height));
            config.AppSettings.Settings.Add("url", Program.url);
            config.AppSettings.Settings.Add("username", Program.username);
            config.AppSettings.Settings.Add("domain", Program.domain);
            config.AppSettings.Settings.Add("project_id", Convert.ToString(Program.project_id));

            if (Program.save_password == "1")
            {
                config.AppSettings.Settings.Add("password", Program.password);
                config.AppSettings.Settings.Add("save_password", "1");
            }
            else
            {
                config.AppSettings.Settings.Add("password", "");
                config.AppSettings.Settings.Add("save_password", "0");
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Program.url = this.textBoxUrl.Text;
            Program.username = this.textBoxUsername.Text;
            Program.password = this.textBoxPassword.Text;
            Program.domain = this.textBoxDomain.Text;
            Program.save_password = this.checkBoxSavePassword.Checked ? "1" : "0";
            try
            {
                Program.project_id = Convert.ToInt32(this.textBoxProjectNumber.Text);
            }
            catch (Exception)
            {
                Program.project_id = 0;
            }

            WriteConfig();

            Close();
        }
    }
}
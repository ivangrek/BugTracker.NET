namespace btnet
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://ifdefined.com/bugtrackernet.html");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
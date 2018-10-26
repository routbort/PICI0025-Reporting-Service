using PICI0025_Reporting_Service;
using System;
using System.Windows.Forms;

namespace TestApplication
{
    public partial class frmMain: Form
    {
        public frmMain()
        {
            InitializeComponent();
            _tw = new ThreadWorker(Config.GetDefaultConfig());
            _tw.LogEvent += _tw_LogEvent;
        }

        private void _tw_LogEvent(object sender, ThreadWorker.LogInfoEventArgs e)
        {
            this.listBox1.Items.Add(e.Message);
            this.listBox1.SelectedIndex = this.listBox1.Items.Count - 1;
        }

        ThreadWorker _tw;  

        private void button1_Click(object sender, EventArgs e)
        {
          var c  =  Config.GetDefaultConfig();
            _tw.DoWork();
        }
    }
}

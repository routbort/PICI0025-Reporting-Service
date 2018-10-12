using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestApplication
{
    public partial class frmMain: Form
    {
        public frmMain()
        {
            InitializeComponent();
            _tw = new PICI0025_Reporting_Service.ThreadWorker(PICI0025_Reporting_Service.Config.GetDefaultConfig());
            _tw.LogEvent += _tw_LogEvent;
        }

        private void _tw_LogEvent(object sender, PICI0025_Reporting_Service.ThreadWorker.LogInfoEventArgs e)
        {
            this.listBox1.Items.Add(e.Message);
            this.listBox1.SelectedIndex = this.listBox1.Items.Count - 1;
        }

        PICI0025_Reporting_Service.ThreadWorker _tw;  

        private void button1_Click(object sender, EventArgs e)
        {
          var c  =  PICI0025_Reporting_Service.Config.GetDefaultConfig();
            _tw.DoWork();
        }
    }
}

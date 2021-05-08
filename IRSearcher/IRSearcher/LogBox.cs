using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaselineSystem
{

    delegate void SetTextCallback(string text);

    public partial class LogBox : Form
    {
        //private List<string> logs = new List<string>();
        //public BindingList<string> Logs { get => bindingListSource;}
        //private BindingList<string> bindingListSource;
        //BindingSource bs;
        public LogBox()
        {
            InitializeComponent();
            //bindingListSource = new BindingList<string>();
            //bs = new BindingSource(bindingListSource, null);
            //bs.DataSource = logs;
            //lbLogBox.DataSource = bs;
        }

        
        /// <summary>
        /// close log window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// clear log
        /// </summary>
        public void ClearLog()
        {
            this.lbLogBox.Items.Clear();
        }
        /// <summary>
        /// Print logs and display in log box
        /// </summary>
        /// <param name="log"></param>
        public void PrintLog(string log)
        {
            lbLogBox.Invoke(new Action(() => this.lbLogBox.Items.Add(log)));
        }

        private void lbLogBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void LogBox_Load(object sender, EventArgs e)
        {

        }
    }
}

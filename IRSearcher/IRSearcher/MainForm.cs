using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BaselineSystem
{
    public delegate void PrintLogDelegate(string log);

    public partial class MainForm : Form
    {
        LuceneSearchEngine luceneSearchEngine = new LuceneSearchEngine();
        LogBox logBox;
        List<Result> results;
        public MainForm()
        {
            InitializeComponent();
            tbCollectionPath.DataBindings.Add(new Binding("Text", luceneSearchEngine, "CollectionPath", false, DataSourceUpdateMode.OnPropertyChanged, string.Empty));
            tbIndexPath.DataBindings.Add(new Binding("Text", luceneSearchEngine, "IndexPath", false, DataSourceUpdateMode.OnPropertyChanged, string.Empty));
            luceneSearchEngine.LogEvent += LogEvent;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseCollectionPath_Click(object sender, EventArgs e)
        {
            // Open the folder browsing dialog box
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (DialogResult.OK == dlg.ShowDialog())
            {
                tbCollectionPath.Text = dlg.SelectedPath;// Displays the path selected by the user
            }
        }
        /// <summary>
        /// Click the browse index path button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseIndexPath_Click(object sender, EventArgs e)
        {
            // Open the folder browsing dialog box
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (DialogResult.OK == dlg.ShowDialog())
            {
                tbIndexPath.Text = dlg.SelectedPath;
            }
        }
        /// <summary>
        /// Click the build index button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCreateIndex_Click(object sender, EventArgs e)
        {
            
            try
            {
                logBox = new LogBox();
                logBox.Show();
                Thread t = new Thread(luceneSearchEngine.BuildIndex);
                t.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Click the search button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSearch_Click(object sender, EventArgs e)
        {
            logBox = new LogBox();
            logBox.Show();
            results = luceneSearchEngine.Search(tbQueryText.Text, cbPreProcessing.Checked);
            dataGridView1.DataSource = results;
        }

        void LogEvent(object o, LogEventArgs e)
        {
            logBox.PrintLog("["+e.time.ToLongTimeString()+"] " + e.log);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DetailFrom from = new DetailFrom(results[e.RowIndex].passage_text);
            from.Show();
        }

        private void btnSaveResult_Click(object sender, EventArgs e)
        {
            if (tbIdentifier.Text.Length == 0)
            {
                MessageBox.Show("Please enter id!");
                return;
            }
            if(luceneSearchEngine.SaveResults == null)
            {
                MessageBox.Show("Nothing to save!");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "textfile|*.txt";
            dlg.FileName = "result.txt";

            if (DialogResult.OK == dlg.ShowDialog())
            {
                using (StreamWriter textWriter = new StreamWriter(dlg.FileName, true))
                {
                    foreach (string res in luceneSearchEngine.SaveResults)
                    {
                        textWriter.Write(tbIdentifier.Text + " ");
                        textWriter.WriteLine(res);
                        //textWriter.WriteLine(" N9573950_N9800174_N9858598_N8554544_TEAM 666");
                    }
                }
                MessageBox.Show("Results Saved!!");
            }
        }
    }
}

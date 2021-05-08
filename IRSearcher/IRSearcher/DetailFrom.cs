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
    public partial class DetailFrom : Form
    {

        public DetailFrom(string text)
        {
            InitializeComponent();

            webBrowser1.DocumentText = text;
        }
    }
}

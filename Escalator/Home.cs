using System;
using System.Windows.Forms;

namespace Escalator
{
    public partial class Home : Form
    {
        public Home()
        {
            InitializeComponent();
        }

        private void Home_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                VerifyListManager.ConvertExportToVerifyList(openFileDialog1.FileName);
            }
        }
    }
}

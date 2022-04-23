using Escalator.Data;
using Escalator.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private void btnCreateVerifyList_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(openFileDialog1.FileName))
            {
                lblError.Text = "Please upload an order list (in .CSV file format).";
                return;
            }
            if (!radioStairs.Checked && !radioRails.Checked)
            {
                lblError.Text = "Please select an order type.";
                return;
            }
            if (checklistDates.CheckedItems.Count == 0)
            {
                lblError.Text = "Please select at least one Requested Date.";
                return;
            }

            lblError.Text = "";
            List<string> dates = new();
            foreach (var checkedDate in checklistDates.CheckedItems)
            {
                dates.Add(checkedDate.ToString());
            }

            try
            {
                VerifyListManager.CreateVerifyList(
                    openFileDialog1.FileName,
                    dates,
                    radioStairs.Checked
                        ? OrderType.STAIRS
                        : radioRails.Checked
                            ? OrderType.RAILS
                            : OrderType.NONE);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnUploadOrderList_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            lblUpload.Text = "";
            openFileDialog1.FileName = null;
            checklistDates.Items.Clear();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (!openFileDialog1.FileName.EndsWith(".csv"))
                {
                    lblError.Text = "Please upload an order list (in .CSV file format).";
                    openFileDialog1.FileName = null;
                    return;
                }

                if (openFileDialog1.FileName.ToLowerInvariant().Contains("stairs"))
                {
                    radioStairs.Checked = true;
                }
                else if (openFileDialog1.FileName.ToLowerInvariant().Contains("rails"))
                {
                    radioRails.Checked = true;
                }

                List<string> dates = null;

                try
                {
                    dates = VerifyListManager.GetAvailableRequestedDates(openFileDialog1.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                if (dates == null || dates.Count == 0)
                {
                    lblError.Text = "No requested dates were found in the uploaded file.";
                    openFileDialog1.FileName = null;
                    return;
                }

                lblUpload.Text = Path.GetFileName(openFileDialog1.FileName);
                foreach (var date in dates)
                {
                    checklistDates.Items.Add(date);
                }

                if (dates.Count == 1)
                {
                    checklistDates.SetItemChecked(0, true);
                }
            }
        }

        private void btnOpenRules_Click(object sender, EventArgs e)
        {
            string path = string.Empty;
            try
            {
                path = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "Rules.xlsx");
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogManager.Log("Error opening Rules file at path: " + path + ": " + ex.Message);
            }
        }
    }
}

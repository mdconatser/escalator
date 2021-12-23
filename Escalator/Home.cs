using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;
using System.Diagnostics;

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
                var workbook = OpenWorkbook(openFileDialog1.FileName);
                var sheet = workbook.GetSheetAt(0);
                var output = CreateWorkbook();
                var outputSheet = output.CreateSheet();

                List<Order> orders = new List<Order>();

                // Get orders from output
                for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    orders.Add(new Order() 
                    { 
                        Subdivision = sheet.GetRow(rowIndex).Cells[1].ToString(), 
                        LotNumber = sheet.GetRow(rowIndex).Cells[0].ToString() 
                    });
                }

                // Sort to desired output
                orders = orders.OrderBy(x => x.Subdivision).ThenBy(x => x.LotNumber).ToList();

                // Transform to combine subdivisions together
                orders = CombineOrders(orders);

                // Write output header
                var rowCounter = 0;
                var colCounter = 0;

                var row = outputSheet.CreateRow(rowCounter++);
                var col = row.CreateCell(colCounter++);
                col.SetCellValue("Lot");

                col = row.CreateCell(colCounter);
                col.SetCellValue("Subdivision");

                colCounter = 0;

                // Write sorted rows
                foreach (var order in orders)
                {
                    row = outputSheet.CreateRow(rowCounter++);

                    col = row.CreateCell(colCounter++);
                    col.SetCellValue(order.LotNumber);

                    col = row.CreateCell(colCounter++);
                    col.SetCellValue(order.Subdivision);

                    colCounter = 0;
                }

                // Save output
                using (FileStream stream = new FileStream(openFileDialog1.FileName + "_Combined.xlsx", FileMode.Create, FileAccess.Write))
                {
                    output.Write(stream);
                }
                output.Close();
                workbook.Close();
                Process.Start(new ProcessStartInfo(openFileDialog1.FileName + "_Combined.xlsx") { UseShellExecute = true });
            }
        }

        public List<Order> CombineOrders(List<Order> orders)
        {
            var combined = new List<Order>();
            var subdivisionsToLots = orders.GroupBy(x => x.Subdivision);

            foreach (var subdivision in subdivisionsToLots)
            {
                // Check if every single lot is sequential, to shorten the output
                string sequentialLot = null;
                if (subdivision.Count() > 1 && subdivision.All(x => int.TryParse(x.LotNumber, out int result)))
                {
                    int? prevLotValue = null;
                    int? firstLotValue = null; // Note if same lot is encountered twice, throw some error
                    int? finalLotValue = null;

                    foreach (var lot in subdivision)
                    {
                        if (!prevLotValue.HasValue || prevLotValue.Value + 1 == Convert.ToInt32(lot.LotNumber))
                        {
                            if (prevLotValue == null)
                            {
                                firstLotValue = Convert.ToInt32(lot.LotNumber);
                                prevLotValue = firstLotValue;
                            }
                            else
                            {
                                finalLotValue = Convert.ToInt32(lot.LotNumber);
                                prevLotValue = finalLotValue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Write lot as "1 - 5"
                    if (firstLotValue.HasValue && finalLotValue.HasValue)
                    {
                        sequentialLot = firstLotValue + " - " + finalLotValue;
                    }
                }

                // Create combined order
                combined.Add(new Order()
                {
                    Subdivision = subdivision.Key,
                    LotNumber = sequentialLot ?? string.Join(", ", subdivision.Select(x => x.LotNumber))
                });
            }

            return combined;
        }

        public IWorkbook CreateWorkbook()
        {
            return new XSSFWorkbook();
        }

        // Attemps to read workbook as XLSX, then XLS, then fails.
        public IWorkbook OpenWorkbook(string path)
        {
            IWorkbook book;

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Try to read workbook as XLSX:
            try
            {
                book = new XSSFWorkbook(fs);
            }
            catch
            {
                book = null;
            }

            // If reading fails, try to read workbook as XLS:
            if (book == null)
            {
                book = new HSSFWorkbook(fs);
            }

            return book;
        }
    }
}

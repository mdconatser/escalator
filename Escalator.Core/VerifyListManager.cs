using CsvHelper;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Escalator
{
    public static class VerifyListManager
    {
        public static void ConvertExportToVerifyList(string path)
        {
            List<Order> orders = ImportOrders(path);
            orders = orders.OrderBy(x => x.Subdivision).ThenBy(x => x.Lot).ToList();
            orders = CombineOrders(orders);

            string outputPath = WriteVerifyList(orders, path);
            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }

        public static List<Order> CombineOrders(List<Order> orders)
        {
            var combined = new List<Order>();
            var subdivisionsToLots = orders.GroupBy(x => x.Subdivision);

            foreach (var subdivision in subdivisionsToLots)
            {
                // Check if every single lot is sequential, to shorten the output
                string sequentialLot = null;
                if (subdivision.Count() > 1 && subdivision.All(x => int.TryParse(x.Lot, out int result)))
                {
                    int? prevLotValue = null;
                    int? firstLotValue = null; // Note if same lot is encountered twice, throw some error
                    int? finalLotValue = null;

                    foreach (var lot in subdivision)
                    {
                        if (!prevLotValue.HasValue || prevLotValue.Value + 1 == Convert.ToInt32(lot.Lot))
                        {
                            if (prevLotValue == null)
                            {
                                firstLotValue = Convert.ToInt32(lot.Lot);
                                prevLotValue = firstLotValue;
                            }
                            else
                            {
                                finalLotValue = Convert.ToInt32(lot.Lot);
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
                    Lot = sequentialLot ?? string.Join(", ", subdivision.Select(x => x.Lot))
                });
            }

            return combined;
        }

        // Attemps to read workbook as XLSX, then XLS, then fails.
        public static List<Order> ImportOrders(string path)
        {
            List<Order> records;
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
                try
                {
                    book = new HSSFWorkbook(fs);
                }
                catch
                {
                    book = null;
                }
            }

            // If reading fails, try to read workbook as CSV:
            if (book == null)
            {
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    records = csv.GetRecords<Order>().ToList();
                }
            }
            else
            {
                var sheet = book.GetSheetAt(0);

                records = new List<Order>();

                // Get orders from input, skip header row
                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    records.Add(new Order()
                    {
                        Lot = sheet.GetRow(rowIndex).Cells[Convert.ToInt32(3)].ToString(),
                        Subdivision = sheet.GetRow(rowIndex).Cells[Convert.ToInt32(4)].ToString()
                    });
                }

                book.Close();
            }

            return records;
        }

        public static string WriteVerifyList(List<Order> orders, string path)
        {
            var output = new XSSFWorkbook();
            var outputSheet = output.CreateSheet();

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
                col.SetCellValue(order.Lot);

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.Subdivision);

                colCounter = 0;
            }

            // Save output
            var outputPath = path + "_Combined.xlsx";
            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                output.Write(stream);
            }
            output.Close();

            return outputPath;
        }
    }
}

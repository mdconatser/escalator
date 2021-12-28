using CsvHelper;
using CsvHelper.Configuration;
using Escalator.Data;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Escalator
{
    public static class VerifyListManager
    {
        public static void ConvertExportToVerifyList(string path, List<string> requestedDates, OrderType orderType)
        {
            List<Order> orders = ImportOrders(path, orderType);
            var errors = ValidateOrders(orders);
            orders = orders.Where(x => requestedDates.Contains(x.RequestedDate)).ToList();
            orders = orders.OrderBy(x => x.Subdivision).ThenBy(x => x.Lot).ToList();
            orders = CombineOrders(orders);
            string outputPath = WriteVerifyList(orders, errors, path);
            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }

        public static List<string> GetAvailableRequestedDates(string path)
        {
            List<Order> orders = ImportOrders(path, OrderType.NONE);
            return orders.Select(x => x.RequestedDate).Distinct().OrderBy(x => x).ToList();
        }

        public static List<string> GetKnownAddressSubdivisions()
        {
            return Properties.Settings.Default.UseAddress.Split(new string[] { ",", ", ", " , ", " ," }, StringSplitOptions.TrimEntries).ToList();
        }

        public static bool ShouldUseAddress(string subdivision)
        {
            return string.IsNullOrWhiteSpace(subdivision) || GetKnownAddressSubdivisions().Contains(subdivision);
        }

        public static List<string> ValidateOrders(List<Order> orders)
        {
            List<string> errors = new();

            // Check for the same lot appearing twice in the same subdivision
            foreach (var subdivision in orders.Where(x => !x.IsSpotLot).GroupBy(x => x.Subdivision))
            {
                foreach (var lot in subdivision.Select(x => x.Lot).Distinct())
                {
                    if (subdivision.Where(x => x.Lot == lot).Count() > 1)
                    {
                        errors.Add($"Duplicate lot \"{lot}\" found in subdivision \"{subdivision.Key}");
                    }
                }
            }

            // Check for the same spot lot address appearing twice
            foreach (var address in orders.Where(x => x.IsSpotLot).GroupBy(x => x.Address))
            {
                if (address.Count() > 1)
                {
                    errors.Add($"Duplicate spot lot address \"{address.Key}\" found");
                }
            }

            return errors;
        }

        public static List<Order> CombineOrders(List<Order> orders)
        {
            var combined = new List<Order>();

            foreach (var subdivision in orders.Where(x => !x.IsSpotLot).GroupBy(x => x.Subdivision))
            {
                // Check if every single lot is sequential, to shorten the output
                string sequentialLot = null;

                if (subdivision.Count() > 1)
                {
                    int? prevLotValue = null;

                    string firstLot = null;
                    string finalLot = null;

                    foreach (var lot in subdivision.GroupBy(x => x.Lot))
                    {
                        int lotValue = Encoding.ASCII.GetBytes(lot.Key).Select(x => (int)x).Sum();
                        if (!prevLotValue.HasValue || prevLotValue.Value + 1 == lotValue)
                        {
                            if (prevLotValue == null)
                            {
                                firstLot = lot.Key;
                                prevLotValue = lotValue;
                            }
                            else
                            {
                                finalLot = lot.Key;
                                prevLotValue = lotValue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Write lot as "1 - 5" or "X1 - X5"
                    if (firstLot != null && finalLot != null)
                    {
                        sequentialLot = firstLot + " - " + finalLot;
                    }
                }

                // Create combined order
                combined.Add(new Order()
                {
                    Subdivision = subdivision.Key,
                    Lot = sequentialLot ?? string.Join(", ", subdivision.Select(x => x.Lot)),
                    Address = subdivision.First().Address,
                    IsSpotLot = false,
                    OrderType = subdivision.First().OrderType
                });
            }

            // Set spot lots and add them to output list
            foreach (var spotLot in orders.Where(x => x.IsSpotLot).OrderBy(x => x.Address).GroupBy(x => x.Address))
            {
                // Create combined order for all of the same address
                combined.Add(new Order()
                {
                    IsSpotLot = true,
                    Address = spotLot.First().Address,
                    Subdivision = spotLot.First().Subdivision,
                    Lot = spotLot.First().Lot,
                    OrderType = spotLot.First().OrderType
                });
            }

            return combined;
        }

        // Attemps to read workbook as XLSX, then XLS, then fails.
        public static List<Order> ImportOrders(string path, OrderType orderType)
        {
            List<Order> records = new();
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
                var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, config))
                {
                    csv.Read();
                    csv.ReadHeader();

                    while (csv.Read())
                    {
                        var record = new Order
                        {
                            Lot = csv.GetField("Lot"),
                            Subdivision = csv.GetField("Subdivision"),
                            Address = csv.GetField("Address"),
                            RequestedDate = csv.GetField("Requested Date"),
                            IsSpotLot = ShouldUseAddress(csv.GetField("Subdivision")),
                            OrderType = orderType
                        };
                        records.Add(record);
                    }
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

        public static string WriteVerifyList(List<Order> orders, List<string> errors, string path)
        {
            var output = new XSSFWorkbook();
            var outputSheet = output.CreateSheet("Verify List");

            var rowCounter = 0;
            var colCounter = 0;
            var highestCol = 0;

            // Write sorted rows
            foreach (var order in orders)
            {
                var row = outputSheet.CreateRow(rowCounter++);
                var col = row.CreateCell(colCounter++);
                col.SetCellValue(order.IsSpotLot ? "" : order.Subdivision);

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.VerifyText);

                highestCol = colCounter;
                colCounter = 0;
            }

            for (int i = 0; i <= 4; i++)
            {
                outputSheet.AutoSizeColumn(i);
            }

            // Write errors on same sheet to the right
            ICellStyle errorStyle = output.CreateCellStyle();
            XSSFFont font = (XSSFFont)output.CreateFont();
            font.Color = HSSFColor.Red.Index;
            errorStyle.SetFont(font);

            for (int i = 0; i < errors.Count; i++)
            {
                IRow row = i < rowCounter ? outputSheet.GetRow(i) : outputSheet.CreateRow(rowCounter++);
                var col = row.CreateCell(highestCol + 2);
                col.CellStyle = errorStyle;

                col.SetCellValue(errors[i]);
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

using Escalator.Data;
using Escalator.Managers;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Escalator
{
    public static class VerifyListManager
    {
        public static void CreateVerifyList(string path, List<string> requestedDates, OrderType orderType)
        {
            var subdivisionWordReplacements = RuleManager.GetSubdivisionWordReplacements();

            List<Order> orders = OrderManager.ImportOrders(path, orderType);
            orders = orders.Where(x => requestedDates.Contains(x.RequestedDate)).ToList();
            orders = RuleManager.ApplyOrderRules(orders);
            orders = orders.OrderBy(x => x.FinalSubdivision).ThenBy(x => x.Lot).ThenBy(x => x.Address).ThenBy(x => x.CustomerPONumber).ToList();

            List<Order> skippedOrders = orders.Where(x => x.Rules.SkipProcessing).ToList();
            orders = orders.Where(x => !x.Rules.SkipProcessing).ToList();

            orders = OrderManager.CombineOrders(orders, subdivisionWordReplacements)
                .Concat(skippedOrders)
                .OrderBy(x => x.ImportSequenceNumber)
                .ToList();

            string outputPath = WriteVerifyList(orders, path);
            LogManager.Clear();
            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }

        public static List<string> GetAvailableRequestedDates(string path)
        {
            List<Order> orders = OrderManager.ImportOrders(path, OrderType.NONE);
            return orders.Select(x => x.RequestedDate).Distinct().OrderBy(x => x).ToList();
        }
        private static string WriteVerifyList(List<Order> orders, string path)
        {
            var output = new XSSFWorkbook();
            var outputSheet = output.CreateSheet("Verify List");

            WriteHeaderRow(outputSheet, new List<Tuple<string, int>>()
            {
                new Tuple<string, int>("Raw", 4),
                new Tuple<string, int>("Adj. Email", 10),
                new Tuple<string, int>("Adj. Sub", 8),
                new Tuple<string, int>("Use PO", 7),
                new Tuple<string, int>("Subdivision", 40),
                new Tuple<string, int>("Requested Date", 15),
                new Tuple<string, int>("Email", 30),
                new Tuple<string, int>("Verify Text", 60)
            });

            ICellStyle errorStyle = output.CreateCellStyle();
            XSSFFont font = (XSSFFont)output.CreateFont();
            font.Color = HSSFColor.Red.Index;
            errorStyle.SetFont(font);

            ICellStyle warningStyle = output.CreateCellStyle();
            warningStyle.FillForegroundColor = HSSFColor.Yellow.Index;
            warningStyle.FillPattern = FillPattern.SolidForeground;

            var rowCounter = 1;
            var colCounter = 0;
            var highestCol = 0;

            // Write sorted rows
            foreach (var order in orders)
            {
                var row = outputSheet.CreateRow(rowCounter++);

                var col = row.CreateCell(colCounter++);
                col.SetCellValue(order.Rules.SkipProcessing ? "X" : "");
                if (col.StringCellValue == "X") { col.CellStyle = warningStyle; }

                col = row.CreateCell(colCounter++);
                col.SetCellValue(!string.IsNullOrWhiteSpace(order.Rules.UpdatedEmail) ? "X" : "");
                if (col.StringCellValue == "X") { col.CellStyle = warningStyle; }

                col = row.CreateCell(colCounter++);
                col.SetCellValue(!string.IsNullOrWhiteSpace(order.Rules.UpdatedSubdivision) ? "X" : "");
                if (col.StringCellValue == "X") { col.CellStyle = warningStyle; }

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.Rules.UsePONumber ? "X" : "");
                if (col.StringCellValue == "X") { col.CellStyle = warningStyle; }

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.IsSpotLot ? "" : order.FinalSubdivision);

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.RequestedDate);

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.FinalEmail);

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.VerifyText);

                highestCol = colCounter;
                colCounter = 0;
            }

            // Write errors on same sheet to the right
            var logs = LogManager.GetLogs();

            for (int i = 0; i < logs.Count; i++)
            {
                var row = i < rowCounter ? outputSheet.GetRow(i) : outputSheet.CreateRow(rowCounter++);
                var col = row.CreateCell(highestCol);
                col.CellStyle = errorStyle;

                col.SetCellValue(logs[i]);
            }

            // Save output
            var outputPath = path + "_VerifyList.xlsx";
            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                output.Write(stream);
            }
            output.Close();

            return outputPath;
        }
        private static void WriteHeaderRow(ISheet sheet, List<Tuple<string, int>> columnNamesToWidths)
        {
            var rowCounter = 0;
            var colCounter = 0;

            var row = sheet.CreateRow(rowCounter++);

            for (int i = 0; i < columnNamesToWidths.Count; i++)
            {
                var col = row.CreateCell(colCounter++);
                col.SetCellValue(columnNamesToWidths[i].Item1);
                sheet.SetColumnWidth(i, columnNamesToWidths[i].Item2 * 256 + 200);
            }
        }
    }
}

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
using System.Text.RegularExpressions;

namespace Escalator
{
    public static class VerifyListManager
    {
        public static void ConvertExportToVerifyList(string path, List<string> requestedDates, OrderType orderType)
        {
            var subdivisionWordReplacements = GetSubdivisionWordReplacements();

            List<Order> orders = ImportOrders(path, orderType);
            orders = orders.Where(x => requestedDates.Contains(x.RequestedDate)).ToList();
            orders = ApplyOrderRules(orders);
            orders = orders.OrderBy(x => x.FinalSubdivision).ThenBy(x => x.Lot).ThenBy(x => x.Address).ThenBy(x => x.CustomerPONumber).ToList();
            var errors = ValidateOrders(orders);

            orders = CombineOrders(orders, subdivisionWordReplacements);

            string outputPath = WriteVerifyList(orders, errors, path);
            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }

        public static List<string> GetAvailableRequestedDates(string path)
        {
            List<Order> orders = ImportOrders(path, OrderType.NONE);
            return orders.Select(x => x.RequestedDate).Distinct().OrderBy(x => x).ToList();
        }

        public static List<string> ValidateOrders(List<Order> orders)
        {
            List<string> errors = new();

            // Check for any rules that specifically say to display an error
            foreach (var order in orders.Where(x => x.Rules.ShowError))
            {
                errors.Add($"Your Rules.xlsx found: {order.Company} / {order.Subdivision} / {order.Lot} / {order.Address}");
            }

            // Check for the same lot appearing twice in the same subdivision
            foreach (var subdivision in orders.Where(x => !x.IsSpotLot).GroupBy(x => x.FinalSubdivision))
            {
                foreach (var lot in subdivision.Select(x => x.Lot).Distinct())
                {
                    if (subdivision.Where(x => x.Lot == lot).Count() > 1)
                    {
                        errors.Add($"Duplicate lot \"{lot}\" found in subdivision \"{subdivision.Key}");
                    }
                }

                // Check for multiple rules being applied within the same subdivision
                if (subdivision.Select(x => x.Rules).Where(x => x.Enabled).Distinct().Count() > 1)
                {
                    errors.Add($"Multiple rules were attempted to be applied to subdivision \"{subdivision.Key}\". Please double-check the output for this subdivision.");
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

        public static List<Order> CombineOrders(List<Order> orders, Dictionary<string, string> subdivisionWordReplacements)
        {
            var combined = new List<Order>();

            var subdivisionGroups = orders.Where(x => !x.IsSpotLot && !x.Rules.UsePONumber).GroupBy(x => x.FinalSubdivision);
            var spotLotGroups = orders.Where(x => x.IsSpotLot && !x.Rules.UsePONumber).OrderBy(x => x.Address).GroupBy(x => x.Address);
            var poNumberGroups = orders.Where(x => x.Rules.UsePONumber).GroupBy(x => x.Company);
            var orderGroupLists = new List<IEnumerable<IGrouping<string, Order>>>() { subdivisionGroups, spotLotGroups, poNumberGroups };

            foreach (var orderGroupList in orderGroupLists)
            {
                foreach (var orderGroup in orderGroupList)
                {
                    var order = (Order)orderGroup.First().Clone();
                    order.Subdivision = GetFormattedSubdivisionName(orderGroup.First().FinalSubdivision, subdivisionWordReplacements);
                    order.Lot = GetShorthandList(orderGroup.Select(x => x.Lot).ToList());
                    order.CustomerPONumber = GetShorthandList(orderGroup.Select(x => x.CustomerPONumber).ToList());
                    combined.Add(order);
                }
            }

            return combined;
        }

        public static string GetShorthandList(List<string> items)
        {
            // Check if the items contain sequential subsets in either character or numeric form
            string shorthandList = null;

            if (items.Count() > 1)
            {
                int? prevItemValue = null;

                string firstItem = null;
                string finalItem = null;

                foreach (var item in items.OrderBy(x => x).GroupBy(x => x).ToList())
                {
                    int itemValueText = Encoding.ASCII.GetBytes(new string(item.Key.Where(c => !char.IsDigit(c)).ToArray())).Select(x => (int)x).Sum();
                    int.TryParse(new string(item.Key.Where(c => char.IsDigit(c)).ToArray()), out int itemValueNumeric);
                    int itemValue = itemValueText + itemValueNumeric;

                    if (!prevItemValue.HasValue || prevItemValue.Value + 1 == itemValue)
                    {
                        if (prevItemValue == null)
                        {
                            firstItem = item.Key;
                            prevItemValue = itemValue;
                        }
                        else
                        {
                            finalItem = item.Key;
                            prevItemValue = itemValue;
                        }
                    }
                    else
                    {
                        shorthandList = firstItem + (finalItem != null ? " - " + finalItem : "") + (shorthandList != null ? " & " : "") + shorthandList;

                        firstItem = item.Key;
                        prevItemValue = itemValue;
                        finalItem = null;
                    }
                }

                shorthandList = shorthandList + (shorthandList != null ? " & " : "") + firstItem + (finalItem != null ? " - " + finalItem : "");
            }

            if (shorthandList == null)
            {
                shorthandList = string.Join(", ", items);
            }

            return shorthandList;
        }

        public static string GetFormattedSubdivisionName(string subdivision, Dictionary<string, string> wordReplacements)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            var subdivisionTokens = textInfo.ToTitleCase(subdivision.ToLowerInvariant()).Split(' ');

            for (int i = 0; i < subdivisionTokens.Count(); i++)
            {
                if (wordReplacements.ContainsKey(subdivisionTokens[i]))
                {
                    subdivisionTokens[i] = subdivisionTokens[i].ReplaceWholeWord(subdivisionTokens[i], wordReplacements[subdivisionTokens[i]]);
                }
            }    

            return string.Join(' ', subdivisionTokens);
        }

        static public string ReplaceWholeWord(this string original, string wordToFind, string replacement, RegexOptions regexOptions = RegexOptions.None)
        {
            string pattern = string.Format(@"\b{0}\b", wordToFind);
            string ret = Regex.Replace(original, pattern, replacement, regexOptions);
            return ret;
        }

        // Attemps to read workbook as XLSX, then XLS, then fails.
        public static List<Order> ImportOrders(string path, OrderType orderType)
        {
            List<Order> records = new();
            //IWorkbook book;

            //FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //// Try to read workbook as XLSX:
            //try
            //{
            //    book = new XSSFWorkbook(fs);
            //}
            //catch
            //{
            //    book = null;
            //}

            //// If reading fails, try to read workbook as XLS:
            //if (book == null)
            //{
            //    try
            //    {
            //        book = new HSSFWorkbook(fs);
            //    }
            //    catch
            //    {
            //        book = null;
            //    }
            //}

            // If reading fails, try to read workbook as CSV:
            //if (book == null)
            //{
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
                            OrderID = csv.GetCleanedField("Order ID"),
                            Lot = csv.GetCleanedField("Lot"),
                            Subdivision = csv.GetCleanedField("Subdivision"),
                            Address = csv.GetCleanedField("Address"),
                            RequestedDate = csv.GetCleanedField("Requested Date"),
                            Company = csv.GetCleanedField("Company"),
                            Email = csv.GetCleanedField("Email"),
                            CustomerPONumber = csv.GetCleanedField("Customer PO#"),
                            OrderType = orderType
                        };

                        if (!string.IsNullOrWhiteSpace(record.OrderID))
                        {
                            records.Add(record); //todo: add error warning if SOME fields existed but not the order ID
                        }
                    }
                }
            //}
            //else
            //{
            //    var sheet = book.GetSheetAt(0);

            //    records = new List<Order>();

            //    // Get orders from input, skip header row
            //    for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            //    {
            //        records.Add(new Order()
            //        {
            //            Lot = sheet.GetRow(rowIndex).Cells[Convert.ToInt32(3)].ToString(),
            //            Subdivision = sheet.GetRow(rowIndex).Cells[Convert.ToInt32(4)].ToString()
            //        });
            //    }

            //    book.Close();
            //}

            return records;
        }

        public static string GetCleanedField(this CsvReader csv, string field)
        {
            return Regex.Replace(csv.GetField(field).Trim(), @"\s+", " ").Replace("�", "");
        }

        public static string WriteVerifyList(List<Order> orders, List<string> errors, string path)
        {
            var output = new XSSFWorkbook();
            var outputSheet = output.CreateSheet("Verify List");

            WriteHeaderRow(outputSheet, new List<Tuple<string, int>>()
            {
                new Tuple<string, int>("Email Rule", 5),
                new Tuple<string, int>("PO Rule", 5),
                new Tuple<string, int>("Subdivision", 40),
                new Tuple<string, int>("Requested Date", 20),
                new Tuple<string, int>("Email", 30),
                new Tuple<string, int>("Verify Text", 60)
            });

            var rowCounter = 1;
            var colCounter = 0;
            var highestCol = 0;

            // Write sorted rows
            foreach (var order in orders)
            {
                var row = outputSheet.CreateRow(rowCounter++);

                var col = row.CreateCell(colCounter++);
                col.SetCellValue(!string.IsNullOrWhiteSpace(order.Rules.UpdatedEmail) ? "X" : "");

                col = row.CreateCell(colCounter++);
                col.SetCellValue(order.Rules.UsePONumber ? "X" : "");

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
            ICellStyle errorStyle = output.CreateCellStyle();
            XSSFFont font = (XSSFFont)output.CreateFont();
            font.Color = HSSFColor.Red.Index;
            errorStyle.SetFont(font);

            for (int i = 0; i < errors.Count; i++)
            {
                var row = i < rowCounter ? outputSheet.GetRow(i) : outputSheet.CreateRow(rowCounter++);
                var col = row.CreateCell(highestCol + 2);
                col.CellStyle = errorStyle;

                col.SetCellValue(errors[i]);
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

        public static void WriteHeaderRow(ISheet sheet, List<Tuple<string, int>> columnNamesToWidths)
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

        public static Dictionary<string, string> GetSubdivisionWordReplacements()
        {
            Dictionary<string, string> wordReplacements = new();

            IWorkbook book;

            FileStream fs = new FileStream("Rules.xlsx", FileMode.Open, FileAccess.Read);
            book = new XSSFWorkbook(fs);

            var sheet = book.GetSheetAt(1);

            // Get orders from input, skip header row
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                wordReplacements.Add(
                    row.Cells[0].ToString(),
                    row.Cells[1].ToString()
                );
            }

            book.Close();

            return wordReplacements;
        }

        public static List<Order> ApplyOrderRules(List<Order> orders)
        {
            List<Rule> rules = new();

            IWorkbook book;

            FileStream fs = new FileStream("Rules.xlsx", FileMode.Open, FileAccess.Read);
            book = new XSSFWorkbook(fs);

            var sheetRules = book.GetSheetAt(0);

            // Get orders from input, skip header rows
            for (int rowIndex = 2; rowIndex <= sheetRules.LastRowNum; rowIndex++)
            {
                var row = sheetRules.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                rules.Add(new Rule() {
                    Enabled = true,
                    Company = row.GetCell(0)?.ToString(),
                    Subdivision = row.GetCell(1)?.ToString(),
                    Lot = row.GetCell(2)?.ToString(),
                    Address = row.GetCell(3)?.ToString(),
                    OriginalEmail = row.GetCell(4)?.ToString(),
                    UpdatedEmail = row.GetCell(5)?.ToString(),
                    UpdatedSubdivision = row.GetCell(6)?.ToString(),
                    SkipProcessing = string.IsNullOrWhiteSpace(row.GetCell(7)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(7)?.ToString()),
                    ShowError = string.IsNullOrWhiteSpace(row.GetCell(8)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(8)?.ToString()),
                    UsePONumber = string.IsNullOrWhiteSpace(row.GetCell(9)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(9)?.ToString())
                });
            }

            book.Close();

            // Set rule object for each order, if available
            foreach (var order in orders)
            {
                foreach (var rule in rules)
                {
                    if (rule.Matches(order))
                    {
                        order.Rules = rule;
                    }
                }
            }

            return orders;
        }
    }
}

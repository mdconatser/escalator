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
            orders = orders.OrderBy(x => x.Subdivision).ThenBy(x => x.Lot).ThenBy(x => x.Address).ToList();
            orders = ApplyOrderRules(orders);
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

            // Check for any rules that specifically say to display an error
            foreach (var order in orders.Where(x => x.Rules?.ShowError ?? false))
            {
                errors.Add($"Your Rules.xlsx found: {order.Company} / {order.Subdivision} / {order.Lot} / {order.Address}");
            }

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

                // Check for multiple rules being applied within the same subdivision
                if (subdivision.Select(x => x.Rules).Distinct().Count() > 1)
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

            foreach (var subdivision in orders.Where(x => !x.IsSpotLot).GroupBy(x => x.Subdivision))
            {
                // Check if the lots contain sequential subsets in either character or numeric form
                string sequentialLot = null;

                if (subdivision.Count() > 1)
                {
                    int? prevLotValue = null;

                    string firstLot = null;
                    string finalLot = null;

                    foreach (var lot in subdivision.OrderBy(x => x.Lot).GroupBy(x => x.Lot).ToList())
                    {
                        // Use value of the lot itself if it is a number, otherwise use the 
                        int lotValueText = Encoding.ASCII.GetBytes(new string(lot.Key.Where(c => !char.IsDigit(c)).ToArray())).Select(x => (int)x).Sum();
                        int lotValueNumeric = Convert.ToInt32(new string(lot.Key.Where(c => char.IsDigit(c)).ToArray()));
                        int lotValue = lotValueText + lotValueNumeric;

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
                            sequentialLot = sequentialLot + (sequentialLot != null ? " & " : "") + firstLot + (finalLot != null ? " - " + finalLot : "");

                            firstLot = lot.Key;
                            prevLotValue = lotValue;
                            finalLot = null;
                        }
                    }

                    sequentialLot = sequentialLot + (sequentialLot != null ? " & " : "") + firstLot + (finalLot != null ? " - " + finalLot : "");
                }

                // Create combined order
                combined.Add(new Order()
                {
                    Subdivision = GetFormattedSubdivisionName(subdivision.Key, subdivisionWordReplacements),
                    Lot = sequentialLot ?? string.Join(", ", subdivision.Select(x => x.Lot)),
                    Address = subdivision.First().Address,
                    IsSpotLot = false,
                    Company = subdivision.First().Company,
                    Email = subdivision.First().Email,
                    OrderType = subdivision.First().OrderType,
                    Rules = subdivision.First().Rules // todo: make it possible for this rules object to be part of the split, to allow more granular control
                });
            }

            // Set spot lots and add them to output list
            foreach (var spotLot in orders.Where(x => x.IsSpotLot).OrderBy(x => x.Address).GroupBy(x => x.Address).ToList())
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
            string pattern = String.Format(@"\b{0}\b", wordToFind);
            string ret = Regex.Replace(original, pattern, replacement, regexOptions);
            return ret;
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
                            Lot = csv.GetCleanedField("Lot"),
                            Subdivision = csv.GetCleanedField("Subdivision"),
                            Address = csv.GetCleanedField("Address"),
                            RequestedDate = csv.GetCleanedField("Requested Date"),
                            Company = csv.GetCleanedField("Company"),
                            Email = csv.GetCleanedField("Email"),
                            OrderType = orderType
                        };

                        record.IsSpotLot = ShouldUseAddress(record.Subdivision);
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

        public static string GetCleanedField(this CsvReader csv, string field)
        {
            return Regex.Replace(csv.GetField(field).Trim(), @"\s+", " ").Replace("�", "");
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

                col = row.CreateCell(colCounter++);
                col.SetCellValue(!string.IsNullOrWhiteSpace(order.Rules?.Email) ? order.Rules.Email : order.Email);

                highestCol = colCounter;
                colCounter = 0;
            }

            outputSheet.SetColumnWidth(0, 40 * 256 + 200);
            outputSheet.SetColumnWidth(1, 60 * 256 + 200);
            outputSheet.SetColumnWidth(2, 30 * 256 + 200);

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
            var outputPath = path + "_VerifyList.xlsx";
            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                output.Write(stream);
            }
            output.Close();

            return outputPath;
        }

        public static Dictionary<string, string> GetSubdivisionWordReplacements()
        {
            Dictionary<string, string> wordReplacements = new();

            IWorkbook book;

            FileStream fs = new FileStream("SubdivisionWordReplacements.xlsx", FileMode.Open, FileAccess.Read);
            book = new XSSFWorkbook(fs);

            var sheet = book.GetSheetAt(0);

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

            var sheet = book.GetSheetAt(0);

            // Get orders from input, skip header row
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                rules.Add(new Rule() {
                    Company = row.GetCell(0)?.ToString(),
                    Subdivision = row.GetCell(1)?.ToString(),
                    Lot = row.GetCell(2)?.ToString(),
                    Address = row.GetCell(3)?.ToString(),
                    Email = row.GetCell(4)?.ToString(),
                    SkipProcessing = string.IsNullOrWhiteSpace(row.GetCell(5)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(5)?.ToString()),
                    ShowError = string.IsNullOrWhiteSpace(row.GetCell(6)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(6)?.ToString()),
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

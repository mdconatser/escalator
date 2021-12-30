using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Escalator.Managers
{
    public class RuleManager
    {
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

                rules.Add(new Rule(row));
            }

            book.Close();

            // Set rule object for each order, if available. Merge together the output of multiple matching rules
            // For example, one rule might flag a warning for a subdivision, and another might assign an email address for a company
            foreach (var order in orders)
            {
                List<Rule> matches = new();

                foreach (var rule in rules)
                {
                    if (rule.Matches(order))
                    {
                        matches.Add(rule);
                    }
                }
                
                if (matches.Count > 0)
                {
                    order.Rules = new Rule(order, matches);

                    // Check for any rules that specifically say to display an error
                    if (order.Rules.ShowError)
                    {
                        LogManager.Log($"Your Rules.xlsx matched an order: {order}");
                    }
                }
            }

            return orders;
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
    }
}

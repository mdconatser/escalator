using CsvHelper;
using Escalator.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Escalator
{
    public static class Extensions
    {
        static public string ReplaceWholeWord(this string original, string wordToFind, string replacement, RegexOptions regexOptions = RegexOptions.None)
        {
            string pattern = string.Format(@"\b{0}\b", wordToFind);
            string ret = Regex.Replace(original, pattern, replacement, regexOptions);
            return ret;
        }

        public static string GetCleanedField(this CsvReader csv, string field)
        {
            return Regex.Replace(csv.GetField(field).Trim(), @"\s+", " ").Replace("�", "");
        }

        public static string ToShorthandList(this IEnumerable<string> items, string listLabel, string parent)
        {
            // Check if the items contain sequential subsets in either character or numeric form
            string shorthandList = null;

            if (items.Where(x => !string.IsNullOrWhiteSpace(x)).Count() > 1)
            {
                int? prevItemValue = null;

                string firstItem = null;
                string finalItem = null;

                int maxlen = items.Where(x => !string.IsNullOrWhiteSpace(x)).Max(x => x.Length);
                var stringAndNumericOrder = items.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x.PadLeft(maxlen, '0'));
                foreach (var item in stringAndNumericOrder.GroupBy(x => x).ToList())
                {
                    if (item.Count() > 1)
                    {
                        LogManager.Log($"Duplicate {listLabel} found for {parent}: {item.First()}");
                    }

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
                        shorthandList = shorthandList + (shorthandList != null ? " & " : "") + firstItem + (finalItem != null ? " - " + finalItem : "");

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
    }
}

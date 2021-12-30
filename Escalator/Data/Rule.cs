using Escalator.Managers;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Escalator
{
    public class Rule
    {
        public Rule()
        {

        }

        public Rule(IRow row)
        {
            Enabled = true;
            Company = row.GetCell(0)?.ToString();
            Subdivision = row.GetCell(1)?.ToString();
            Lot = row.GetCell(2)?.ToString();
            Address = row.GetCell(3)?.ToString();
            OriginalEmail = row.GetCell(4)?.ToString();
            UpdatedEmail = row.GetCell(5)?.ToString();
            UpdatedSubdivision = row.GetCell(6)?.ToString();
            SkipProcessing = string.IsNullOrWhiteSpace(row.GetCell(7)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(7)?.ToString());
            ShowError = string.IsNullOrWhiteSpace(row.GetCell(8)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(8)?.ToString());
            UsePONumber = string.IsNullOrWhiteSpace(row.GetCell(9)?.ToString()) ? false : Convert.ToBoolean(row.GetCell(9)?.ToString());
        }

        public Rule(Order order, List<Rule> rules)
        {
            Enabled = rules.Any(x => x.Enabled);

            // Fields that affect output
            UpdatedEmail = MergeField(order, rules.Select(x => x.UpdatedEmail), "Email");
            UpdatedSubdivision = MergeField(order, rules.Select(x => x.UpdatedSubdivision), "Subdivision");
            UsePONumber = rules.Any(x => x.UsePONumber);
            ShowError = rules.Any(x => x.ShowError);
            SkipProcessing = rules.Any(x => x.SkipProcessing);

            // Merged match rules, shouldn't be very useful after this method
            Address = rules.Select(x => x.Address).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            Company = rules.Select(x => x.Company).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            Lot = rules.Select(x => x.Lot).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            OriginalEmail = rules.Select(x => x.OriginalEmail).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            Subdivision = rules.Select(x => x.Subdivision).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        public string Subdivision { get; set; }
        public string Lot { get; set; }
        public string Address { get; set; }
        public string Company { get; set; }
        public string OriginalEmail { get; set; }

        public string UpdatedEmail { get; set; }
        public string UpdatedSubdivision { get; set; }
        public bool SkipProcessing { get; set; }
        public bool ShowError { get; set; }
        public bool UsePONumber { get; set; }
        public bool Enabled { get; set; }

        public bool Matches(Order order)
        {
            List<bool?> checks = new()
            {
                FieldMatches(Address, order.Address),
                FieldMatches(Subdivision, order.Subdivision),
                FieldMatches(Lot, order.Lot),
                FieldMatches(Company, order.Company),
                FieldMatches(OriginalEmail, order.Email),
            };

            return Enabled && checks.Where(x => x.HasValue).All(x => x.Value);
        }

        public bool? FieldMatches(string ruleField, string orderField)
        {
            if (string.IsNullOrWhiteSpace(ruleField))
            {
                return null;
            }

            return ruleField.ToLowerInvariant().Equals(orderField?.ToLowerInvariant())
                || (ruleField == "#blank" && string.IsNullOrWhiteSpace(orderField));
        }

        public static string MergeField(Order order, IEnumerable<string> desired, string fieldName)
        {
            var desiredAddresses = desired.Where(x => !string.IsNullOrWhiteSpace(x));
            if (desiredAddresses.Count() > 1)
            {
                LogManager.Log($"Order {order.OrderID} had conflicting rules to update the {fieldName} field. The last rule took priority, but please check the output, order, and rules for accuracy.");
            }
            return desiredAddresses.FirstOrDefault();
        }
    }
}

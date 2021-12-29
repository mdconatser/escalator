using Escalator.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escalator
{
    public class Rule
    {
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

        //todo: add some auto rule value based on how many fields are filled out, and take the highest rule for that order or do some kind of combination between all rules that apply, but override based on the one with a higher value

        public bool Matches(Order order)
        {
            return (!string.IsNullOrWhiteSpace(Address) && !string.IsNullOrWhiteSpace(order.Address) && order.Address.ToLowerInvariant().Equals(Address.ToLowerInvariant()))
                || (!string.IsNullOrWhiteSpace(Subdivision) && !string.IsNullOrWhiteSpace(Lot) && !string.IsNullOrWhiteSpace(order.Subdivision) && !string.IsNullOrWhiteSpace(order.Lot) && order.Subdivision.ToLowerInvariant().Equals(Subdivision.ToLowerInvariant()) && order.Lot.ToLowerInvariant().Equals(Lot.ToLowerInvariant()))
                || (!string.IsNullOrWhiteSpace(Subdivision) && !string.IsNullOrWhiteSpace(order.Subdivision) && order.Subdivision.ToLowerInvariant().Equals(Subdivision.ToLowerInvariant()))
                || (!string.IsNullOrWhiteSpace(Company) && !string.IsNullOrWhiteSpace(order.Company) && order.Company.ToLowerInvariant().Equals(Company.ToLowerInvariant()))
                || (!string.IsNullOrWhiteSpace(OriginalEmail) && !string.IsNullOrWhiteSpace(order.Email) && order.Email.ToLowerInvariant().Equals(OriginalEmail.ToLowerInvariant()));
        }
    }
}

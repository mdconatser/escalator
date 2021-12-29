using Escalator.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escalator
{
    public class Order : ICloneable
    {
        public Order()
        {
            Rules = new Rule();
        }
        
        public string OrderID { get; set; }
        public string Subdivision { get; set; }
        public string Lot { get; set; }
        public string Address { get; set; }
        public string RequestedDate { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        public string CustomerPONumber { get; set; }

        internal bool IsSpotLot { get { return Rules.UpdatedSubdivision == "#blank" || string.IsNullOrWhiteSpace(Subdivision); } }
        internal OrderType OrderType { get; set; }

        internal string FinalEmail { get { return string.IsNullOrWhiteSpace(Rules.UpdatedEmail) ? Email : Rules.UpdatedEmail == "#blank" ? "" : Rules.UpdatedEmail; } }
        internal string FinalSubdivision { get { return string.IsNullOrWhiteSpace(Rules.UpdatedSubdivision) ? Subdivision : Rules.UpdatedSubdivision == "#blank" ? "" : Rules.UpdatedSubdivision; } }

        internal Rule Rules { get; set; }
        internal string VerifyText 
        { 
            get 
            { 
                return (Rules.UsePONumber
                    ? CustomerPONumber
                    : IsSpotLot 
                        ? Address 
                        : Lot + " " + FinalSubdivision) + " " + OrderType.ToString(); 
            } 
        }

        public object Clone()
        {
            return new Order()
            {
                Subdivision = Subdivision,
                Lot = Lot,
                Address = Address,
                RequestedDate = RequestedDate,
                Company = Company,
                Email = Email,
                CustomerPONumber = CustomerPONumber,
                OrderType = OrderType,
                Rules = Rules
            };
        }
    }
}

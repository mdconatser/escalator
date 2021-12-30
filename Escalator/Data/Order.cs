using CsvHelper;
using Escalator.Data;
using System;

namespace Escalator
{
    public class Order : ICloneable
    {
        public Order()
        {
            Rules = new Rule();
        }

        public Order(CsvReader csv, OrderType orderType)
        {
            Rules = new Rule();

            OrderID = csv.GetCleanedField("Order ID");
            Lot = csv.GetCleanedField("Lot");
            Subdivision = csv.GetCleanedField("Subdivision");
            Address = csv.GetCleanedField("Address");
            RequestedDate = csv.GetCleanedField("Requested Date");
            Company = csv.GetCleanedField("Company");
            Email = csv.GetCleanedField("Email");
            CustomerPONumber = csv.GetCleanedField("Customer PO#");
            OrderType = orderType;
        }
        
        public string OrderID { get; set; }
        public string Subdivision { get; set; }
        public string Lot { get; set; }
        public string Address { get; set; }
        public string RequestedDate { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        public string CustomerPONumber { get; set; }

        internal bool IsSpotLot { get { return string.IsNullOrWhiteSpace(FinalSubdivision); } }
        internal OrderType OrderType { get; set; }

        internal string FinalEmail { get { return string.IsNullOrWhiteSpace(Rules.UpdatedEmail) || Rules.SkipProcessing || Rules.Enabled ? Email : Rules.UpdatedEmail == "#blank" ? "" : Rules.UpdatedEmail; } }
        internal string FinalSubdivision { get { return string.IsNullOrWhiteSpace(Rules.UpdatedSubdivision) || Rules.SkipProcessing || Rules.Enabled ? Subdivision : Rules.UpdatedSubdivision == "#blank" ? "" : Rules.UpdatedSubdivision; } }

        internal bool HasDetails 
        { 
            get 
            {
                return !string.IsNullOrWhiteSpace(OrderID)
                    || !string.IsNullOrWhiteSpace(Subdivision)
                    || !string.IsNullOrWhiteSpace(Lot)
                    || !string.IsNullOrWhiteSpace(Address)
                    || !string.IsNullOrWhiteSpace(Company)
                    || !string.IsNullOrWhiteSpace(RequestedDate)
                    || !string.IsNullOrWhiteSpace(Email)
                    || !string.IsNullOrWhiteSpace(CustomerPONumber);
            } 
        }

        internal Rule Rules { get; set; }
        internal string VerifyText 
        { 
            get 
            { 
                return (Rules.UsePONumber
                    ? "PO " + CustomerPONumber
                    : IsSpotLot 
                        ? Address 
                        : Lot + " " + FinalSubdivision) + " " + OrderType.ToString(); 
            } 
        }

        public object Clone()
        {
            return new Order()
            {
                OrderID = OrderID,
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

        public override string ToString()
        {
            return $"{OrderID} / {Company} / {Subdivision} / {Lot} / {Address} / {Email} / {CustomerPONumber}";
        }
    }
}

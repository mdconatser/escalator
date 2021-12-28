using Escalator.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escalator
{
    public class Order
    {
        public string Subdivision { get; set; }
        public string Lot { get; set; }
        public string Address { get; set; }
        public string RequestedDate { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        internal bool IsSpotLot { get; set; }
        internal OrderType OrderType { get; set; }
        internal string VerifyText { get { return (IsSpotLot ? Address : Lot + " " + Subdivision) + " " + OrderType.ToString(); } }
        internal Rule Rules { get; set; }
    }
}

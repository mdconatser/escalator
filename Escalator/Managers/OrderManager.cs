using CsvHelper;
using CsvHelper.Configuration;
using Escalator.Data;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Escalator.Managers
{
    public class OrderManager
    {
        // Attemps to read workbook as CSV
        public static List<Order> ImportOrders(string path, OrderType orderType)
        {
            List<Order> records = new();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    var record = new Order(csv, orderType);

                    if (record.HasDetails)
                    {
                        if (string.IsNullOrWhiteSpace(record.OrderID))
                        {
                            LogManager.Log($"Found an order with partial details, but it did not have an Order ID. Please check for accuracy.");
                        }

                        records.Add(record);
                    }
                }
            }

            return records;
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
                    order.Subdivision = RuleManager.GetFormattedSubdivisionName(orderGroup.First().FinalSubdivision, subdivisionWordReplacements);
                    order.Lot = orderGroup.Select(x => x.Lot).ToShorthandList("lot", order.Subdivision);
                    order.CustomerPONumber = orderGroup.Select(x => x.CustomerPONumber).ToShorthandList("PO number", order.Company);
                    combined.Add(order);
                }
            }

            return combined;
        }
    }
}

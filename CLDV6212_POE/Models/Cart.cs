using Azure;
using Azure.Data.Tables;

namespace CLDV6212_POE.Models
{
    public class Cart : ITableEntity
    {
        // PartitionKey will be "Cart"
        public string PartitionKey { get; set; } = "Cart";

        // RowKey will be CustomerNumber-ProductNumber
        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Cart attributes
        public string CustomerNumber { get; set; }
        public string ProductNumber { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }
    }
}

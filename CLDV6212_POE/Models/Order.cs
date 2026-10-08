using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CLDV6212_POE.Models
{
    public class Order : ITableEntity
    {
        //------------------------------------------------------------------------------------------//
        //Partion key is used to group the Order entities
        public string PartitionKey { get; set; } = "Order";

        //Row key is set to the Order ID
        public string RowKey { get; set; }

        //Timestamp is a property of ITableEntity and is automatically managed by Azure Table Storage
        [JsonIgnore]
        public DateTimeOffset? Timestamp { get; set; }

        //ETag is used for concurrency checks and is automatically managed by Azure Table Storage
        [JsonIgnore]
        public ETag ETag { get; set; } = default;

        //------------------------------------------------------------------------------------------//
        //Order attributes
        [Display(Name = "Customer ID")]
        public string CustomerNumber { get; set; }

        [Display(Name = "Product ID")]
        public string ProductNumber { get; set; }

        public DateTime? OrderDate { get; set; }

        public int Quantity { get; set; }

        //Added Status property to track the order status between "Pending" and "Completed"
        public string Status { get; set; } = "Proccessed";

        [Display(Name = "Order ID")]
        public string OrderNumber
        {
            get => RowKey;
            set => RowKey = value;
        }
        //End of Order attributes
        //------------------------------------------------------------------------------------------//
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//

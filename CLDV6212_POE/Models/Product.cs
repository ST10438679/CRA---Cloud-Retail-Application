using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace CLDV6212_POE.Models
{
    public class Product : ITableEntity
    {
        //------------------------------------------------------------------------------------------//
        //Partion key is used to group the Product entities
        public string PartitionKey { get; set; } = "Product";

        //Row key is set to the Product ID
        public string RowKey { get; set; }

        //Timestamp is a property of ITableEntity and is automatically managed by Azure Table Storage
        public DateTimeOffset? Timestamp { get; set; }

        //ETag is used for concurrency checks and is automatically managed by Azure Table Storage
        public ETag ETag { get; set; }

        //------------------------------------------------------------------------------------------//
        //Product attributes
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Display(Name = "Product Description")]
        public string ProductDescription { get; set; }

        [Display(Name = "Product Image")]
        public string? ImageURL { get; set; }

        [Display(Name = "Product Promo Video")]
        public string? VideoURL { get; set; }

        [Display(Name = "Product ID")]
        public string ProductNumber
        {
            get => RowKey;
            set => RowKey = value;
        }
        //End of Product attributes
        //------------------------------------------------------------------------------------------//
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
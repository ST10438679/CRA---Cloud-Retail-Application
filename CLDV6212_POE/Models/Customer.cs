using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace CLDV6212_POE.Models
{
    public class Customer : ITableEntity
    {
        //Partion key is used to group the Customer entities
        public string PartitionKey { get; set; } = "Customer";

        //Row key is set to the Customer ID
        public string RowKey { get; set; }

        //Timestamp is a property of ITableEntity and is automatically managed by Azure Table Storage
        public DateTimeOffset? Timestamp { get; set; }

        //ETag is used for concurrency checks and is automatically managed by Azure Table Storage
        public ETag ETag { get; set; }

        //------------------------------------------------------------------------------------------//
        //Customer attributes
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Display(Name = "Customer Surname")]
        public string CustomerSurname { get; set; }

        [Display(Name = "Customer Email")]
        public string CustomerEmail { get; set; }

        [Display(Name = "Customer Password")]
        public string CustomerPassword { get; set; }

        [Display(Name = "Customer ID")]
        public string CustomerNumber
        {
            get => RowKey;
            set => RowKey = value;
        }
        //End of Customer attributes
        //------------------------------------------------------------------------------------------//
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
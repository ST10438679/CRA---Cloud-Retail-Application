namespace CLDV6212_POE.Models
{
    public class Contracts
    {
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// This class serves as a container for lists of Customers and their associated uploaded files
        /// </summary>
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<theFileInfo> UploadedFiles { get; set; } = new List<theFileInfo>();
        //------------------------------------------------------------------------------------------//
    }

    //------------------------------------------------------------------------------------------//
    /// <summary>
    /// Our File Class to associate uploaded files with a customer via CustomerId
    /// </summary>
    public class theFileInfo
    {
        //------------------------------------------------------------------------------------------//
        public string FileName { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string OriginalFileName { get; set; }
        public DateTime UploadDate { get; set; }
        //------------------------------------------------------------------------------------------//
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
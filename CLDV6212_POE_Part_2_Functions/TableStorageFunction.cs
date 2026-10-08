using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CLDV6212_POE_Part_2_Functions;

public class TableStorageFunction
{
    private readonly ILogger<TableStorageFunction> _logger;

    public TableStorageFunction(ILogger<TableStorageFunction> logger)
    {
        _logger = logger;
    }
    //Custoner Entity
    public class Customer : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        //------------------------------------------------------------------------------------------//
        //Customer attributes
        public string CustomerName { get; set; }
        public string CustomerSurname { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPassword { get; set; }
        public string CustomerNumber
        {
            get => RowKey;
            set => RowKey = value;
        }
        //End of Customer attributes
        //------------------------------------------------------------------------------------------//
    }
    //------------------------------------------------------------------------------------------//

    [Function("InsertCustomerToTable")]
    public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var customer = JsonConvert.DeserializeObject<Customer>(requestBody);

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var tableClient = new TableClient(connectionString, "CustomerTable");
        await tableClient.CreateIfNotExistsAsync();

        await tableClient.AddEntityAsync(customer);
        return new OkObjectResult($"Customer {customer.CustomerName} added.");
    }
}
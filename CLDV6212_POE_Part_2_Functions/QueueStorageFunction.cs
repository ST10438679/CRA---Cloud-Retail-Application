using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLDV6212_POE_Part_2_Functions;

public class QueueStorageFunction
{
    private readonly ILogger<QueueStorageFunction> _logger;

    public QueueStorageFunction(ILogger<QueueStorageFunction> logger)
    {
        _logger = logger;
    }

    public class Order : ITableEntity
    {
        //------------------------------------------------------------------------------------------//
        //Order entity for reference in Functions
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        //------------------------------------------------------------------------------------------//
        //Order attributes
        public string CustomerNumber { get; set; }
        public string ProductNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = "Proccessed";
        public string OrderNumber
        {
            get => RowKey;
            set => RowKey = value;
        }
        //End of Order attributes
        //------------------------------------------------------------------------------------------//
    }

    [Function(nameof(QueueSender))]
    public async Task QueueSender([QueueTrigger("orders", Connection = "AzureWebJobsStorage")] QueueMessage message)
    {
        _logger.LogInformation($"Processing: {message.MessageText}");

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var tableClient = new TableClient(connectionString, "OrderTable");
        await tableClient.CreateIfNotExistsAsync();

        var order = JsonSerializer.Deserialize<Order>(message.MessageText);

        if (order == null)
        {
            _logger.LogError("Failed to deserialize the order message.");
            return;
            
        }

        _logger.LogInformation($"Inserting Order {order.OrderNumber} into Table Storage.");

        await tableClient.AddEntityAsync(order);
        _logger.LogInformation($"Order {order.OrderNumber} added to Table Storage.");
    }
}
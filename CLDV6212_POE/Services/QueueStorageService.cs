using Azure.Storage.Queues;

namespace CLDV6212_POE.Services
{
    public class QueueStorageService
    {
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// Our read only holder that connects to the azure storage queue
        /// </summary>
        private readonly QueueClient _queueClient;
        //------------------------------------------------------------------------------------------//

        /// <summary>
        /// Connecting our holder to azure via json file
        /// </summary>
        /// <param name="config"></param>
        public QueueStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            var queueName = "orders";
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        //This code was helped created by IIEVC School of Computer Science youtube channel
        //Link: https://youtube.com/playlist?list=PL480DYS-b_kcZiyuCyHolh6Nad8J_Xnk7&si=b7ghiNAQ1j_vConX
        //This video is unlisted
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// This method sends an order message to the queue
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendOrderAsync(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                await _queueClient.SendMessageAsync(message);
            }
        }

        /// <summary>
        /// This method peeks at the next order message in the queue without removing it
        /// </summary>
        /// <returns></returns>
        public async Task<string?> PeekOrderAsync()
        {
            var peeked = await _queueClient.PeekMessageAsync();
            return peeked?.Value?.MessageText;
        }
        //------------------------------------------------------------------------------------------//
    }
}

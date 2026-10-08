using Azure;
using Azure.Data.Tables;
using CLDV6212_POE.Models;
using Newtonsoft.Json;
using System.Text;

namespace CLDV6212_POE.Services
{
    public class TableStorageService
    {
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// Our tables within azure storage with read only access
        /// </summary>
        private readonly TableClient FirstTableClient;
        private readonly TableClient SecondTableClient;
        private readonly TableClient ThirdTableClient;
        //------------------------------------------------------------------------------------------//

        // Cart Table Client
        private readonly TableClient _cartTableClient;
        private readonly string _insertCustomerFunctionUrl;

        public TableStorageService(IConfiguration config)
        {
            //------------------------------------------------------------------------------------------//
            //Connecting our holders to azure via json file
            var firstConnectionString = config["AzureStorage:ConnectionString"];
            var firstTableName = "CustomerTable";
            FirstTableClient = new TableClient(firstConnectionString, firstTableName);
            FirstTableClient.CreateIfNotExists();

            var SecondConnectionString = config["AzureStorage:ConnectionString"];
            var SecondTableName = "ProductTable";
            SecondTableClient = new TableClient(SecondConnectionString, SecondTableName);
            SecondTableClient.CreateIfNotExists();

            var ThirdConnectionString = config["AzureStorage:ConnectionString"];
            var ThirdTableName = "OrderTable";
            ThirdTableClient = new TableClient(ThirdConnectionString, ThirdTableName);
            ThirdTableClient.CreateIfNotExists();
            //------------------------------------------------------------------------------------------//

            // Cart Table Client Initialization
            var FourthConnectionString = config["AzureStorage:ConnectionString"];
            var FourthTableName = "CartTable";
            _cartTableClient = new TableClient(FourthConnectionString, FourthTableName);
            _cartTableClient.CreateIfNotExists();
            _insertCustomerFunctionUrl = config["AzureFunctions:InsertCustomerToTableUrl"];

        }

        /// <summary>
        /// Our method to insert a customer entity into the customer table
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public async Task InsertCustomerAsync(Customer customer)
        {
            await FirstTableClient.AddEntityAsync(customer);
        }

        /// <summary>
        /// This method list our all customer entities within the customer table
        /// </summary>
        /// <returns></returns>
        public async Task<List<Customer>> GetAllCustomerAsync()
        {
            var customer = new List<Customer>();

            await foreach (Customer entity in FirstTableClient.QueryAsync<Customer>())
            {
                customer.Add(entity);
            }

            return customer;
        }

        //This code was helped created by IIEVC School of Computer Science youtube channel
        //Link: https://youtube.com/playlist?list=PL480DYS-b_kcZiyuCyHolh6Nad8J_Xnk7&si=b7ghiNAQ1j_vConX
        //This video is unlisted
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// This method insert a product entity into the product table
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public async Task InsertProductAsync(Product product)
        {
            await SecondTableClient.AddEntityAsync(product);
        }

        /// <summary>
        /// This method list our all product entities within the product table
        /// </summary>
        /// <returns></returns>
        public async Task<List<Product>> GetAllProductAsync()
        {
            var product = new List<Product>();

            await foreach (Product entity in SecondTableClient.QueryAsync<Product>())
            {
                product.Add(entity);
            }

            return product;
        }

        /// <summary>
        /// This method insert a order entity into the order table
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task InsertOrderAsync(Order order)
        {
            await ThirdTableClient.AddEntityAsync(order);
        }

        /// <summary>
        /// This method list our all order entities within the order table
        /// </summary>
        /// <returns></returns>
        public async Task<List<Order>> GetAllOrderAsync()
        {
            var order = new List<Order>();

            await foreach (Order entity in ThirdTableClient.QueryAsync<Order>())
            {
                order.Add(entity);
            }

            return order;
        }
        //------------------------------------------------------------------------------------------//

        //----------------- Delete Methods ----------------//
        //Helped generated by AI
        //GitHub Copilot within Visual Studio
        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await FirstTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await SecondTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await ThirdTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
        //--------------------------------------------------//

        //----------------- Update Methods ----------------//
        //Helped generated by AI
        //GitHub Copilot within Visual Studio
        public async Task UpdateCustomerAsync(Customer customer)
        {
            await FirstTableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Merge);
        }

        public async Task UpdateProductAsync(Product product)
        {
            await SecondTableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Merge);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            await ThirdTableClient.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Merge);
        }
        //--------------------------------------------------//

        //----------------- Get Single Entity Methods ----------------//
        //Helped generated by AI
        //GitHub Copilot within Visual Studio
        public async Task<Customer> GetCustomerAsync(string partitionKey, string rowKey)
        {
            var response = await FirstTableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
            return response.Value;
        }

        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            var response = await SecondTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
            return response.Value;
        }

        public async Task<Order> GetOrderAsync(string partitionKey, string rowKey)
        {
            var response = await ThirdTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
            return response.Value;
        }
        //--------------------------------------------------//

        //------------------ Azure Function Call Method ----------------//
        // This method calls the Azure Function to insert a customer
        public async Task CallInsertCustomerFunction(Customer model)
        {
            using var httpClient = new HttpClient();

            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Azure Function URL and key
            var functionUrl = _insertCustomerFunctionUrl;

            var response = await httpClient.PostAsync(functionUrl, content);
        }
        //--------------------------------------------------//

        // Cart CRUD Operations
        public async Task<List<Cart>> GetCustomerCartAsync(string customerNumber)
        {
            var cartItems = new List<Cart>();
            await foreach (var item in _cartTableClient.QueryAsync<Cart>(c => c.CustomerNumber == customerNumber))
            {
                cartItems.Add(item);
            }
            return cartItems;
        }

        public async Task AddToCartAsync(Cart cartItem)
        {
            cartItem.RowKey = $"{cartItem.CustomerNumber}-{cartItem.ProductNumber}";
            await _cartTableClient.UpsertEntityAsync(cartItem);
        }

        public async Task UpdateCartItemAsync(Cart cartItem)
        {
            await _cartTableClient.UpdateEntityAsync(cartItem, cartItem.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteCartItemAsync(string customerNumber, string productNumber)
        {
            string rowKey = $"{customerNumber}-{productNumber}";
            await _cartTableClient.DeleteEntityAsync("Cart", rowKey);
        }

        public async Task ClearCustomerCartAsync(string customerNumber)
        {
            var cartItems = await GetCustomerCartAsync(customerNumber);
            foreach (var item in cartItems)
            {
                await _cartTableClient.DeleteEntityAsync("Cart", item.RowKey);
            }
        }

    }
}

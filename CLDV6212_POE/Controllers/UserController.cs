using Microsoft.AspNetCore.Mvc;
using CLDV6212_POE.Models;
using CLDV6212_POE.Services;
using Azure.Data.Tables;

namespace CLDV6212_POE.Controllers
{
    public class UserController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly TableClient _productTableClient;
        private readonly TableClient _orderTableClient;

        public UserController(IConfiguration configuration, TableStorageService tableService)
        {
            _tableService = tableService;

            var ConnectionString = configuration["AzureStorage:ConnectionString"];
            var serviceClient = new TableServiceClient(ConnectionString);

            _productTableClient = serviceClient.GetTableClient("ProductTable");
            _orderTableClient = serviceClient.GetTableClient("OrderTable");
        }

        // Customer Home Page
        public async Task<IActionResult> Index()
        {
            if (!IsCustomerAuthenticated())
                return RedirectToAction("Login", "Account");

            var products = new List<Product>();
            await foreach (var product in _productTableClient.QueryAsync<Product>())
            {
                products.Add(product);
                if (products.Count >= 6) break;
            }

            await UpdateCartCount();
            return View(products);
        }

        // Shop Page
        public async Task<IActionResult> Shop()
        {
            if (!IsCustomerAuthenticated())
                return RedirectToAction("Login", "Account");

            var products = new List<Product>();
            await foreach (var product in _productTableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }

            await UpdateCartCount();
            return View(products);
        }

        // Cart Page
        public async Task<IActionResult> Cart()
        {
            if (!IsCustomerAuthenticated())
                return RedirectToAction("Login", "Account");

            var customerNumber = HttpContext.Session.GetString("CustomerNumber");
            var cartItems = await _tableService.GetCustomerCartAsync(customerNumber);

            // Get full product details for each cart item
            var cartWithProducts = new List<CartItem>();
            foreach (var item in cartItems)
            {
                try
                {
                    var product = await _productTableClient.GetEntityAsync<Product>("Product", item.ProductNumber);
                    cartWithProducts.Add(new CartItem
                    {
                        Product = product.Value,
                        Quantity = item.Quantity
                    });
                }
                catch
                {
                    // Product no longer exists, remove from cart
                    await _tableService.DeleteCartItemAsync(customerNumber, item.ProductNumber);
                }
            }

            ViewBag.CartCount = cartWithProducts.Count;
            return View(cartWithProducts);
        }

        // Add to Cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            if (!IsCustomerAuthenticated())
                return Json(new { success = false, message = "Please login first" });

            try
            {
                var customerNumber = HttpContext.Session.GetString("CustomerNumber");
                var product = await _productTableClient.GetEntityAsync<Product>("Product", productId);

                // Check if item already exists in cart
                var existingCart = await _tableService.GetCustomerCartAsync(customerNumber);
                var existingItem = existingCart.FirstOrDefault(c => c.ProductNumber == productId);

                if (existingItem != null)
                {
                    // Update quantity
                    existingItem.Quantity += quantity;
                    await _tableService.UpdateCartItemAsync(existingItem);
                }
                else
                {
                    // Add new item
                    var cartItem = new Cart
                    {
                        CustomerNumber = customerNumber,
                        ProductNumber = productId,
                        Quantity = quantity,
                        DateAdded = DateTime.UtcNow
                    };
                    await _tableService.AddToCartAsync(cartItem);
                }

                var updatedCart = await _tableService.GetCustomerCartAsync(customerNumber);
                return Json(new { success = true, message = "Product added to cart", cartCount = updatedCart.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding product to cart" });
            }
        }

        // Update Cart Quantity
        [HttpPost]
        public async Task<IActionResult> UpdateCartQuantity(string productId, int quantity)
        {
            if (!IsCustomerAuthenticated())
                return Json(new { success = false, message = "Please login first" });

            try
            {
                var customerNumber = HttpContext.Session.GetString("CustomerNumber");
                var cartItems = await _tableService.GetCustomerCartAsync(customerNumber);
                var item = cartItems.FirstOrDefault(c => c.ProductNumber == productId);

                if (item != null && quantity > 0 && quantity <= 99)
                {
                    item.Quantity = quantity;
                    await _tableService.UpdateCartItemAsync(item);

                    var totalQuantity = cartItems.Sum(c => c.Quantity);
                    return Json(new { success = true, totalQuantity = totalQuantity });
                }

                return Json(new { success = false, message = "Invalid quantity" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating quantity" });
            }
        }

        // Remove from Cart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(string productId)
        {
            if (!IsCustomerAuthenticated())
                return Json(new { success = false, message = "Please login first" });

            try
            {
                var customerNumber = HttpContext.Session.GetString("CustomerNumber");
                await _tableService.DeleteCartItemAsync(customerNumber, productId);

                var updatedCart = await _tableService.GetCustomerCartAsync(customerNumber);
                return Json(new { success = true, message = "Product removed from cart", cartCount = updatedCart.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error removing item" });
            }
        }

        // Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] List<CheckoutItem> cartItems)
        {
            if (!IsCustomerAuthenticated())
                return RedirectToAction("Login", "Account");

            var customerNumber = HttpContext.Session.GetString("CustomerNumber");

            try
            {
                foreach (var item in cartItems)
                {
                    var order = new Order
                    {
                        RowKey = GenerateOrderNumber(),
                        CustomerNumber = customerNumber,
                        ProductNumber = item.ProductId,
                        OrderDate = DateTime.UtcNow,
                        Quantity = item.Quantity,
                        Status = "Completed"
                    };

                    await _orderTableClient.AddEntityAsync(order);
                }

                // Clear cart after successful checkout
                await _tableService.ClearCustomerCartAsync(customerNumber);

                TempData["Success"] = "Order placed successfully!";
                return Ok();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error processing order. Please try again.";
                return BadRequest();
            }
        }

        // Orders Page
        public async Task<IActionResult> Orders()
        {
            if (!IsCustomerAuthenticated())
                return RedirectToAction("Login", "Account");

            var customerNumber = HttpContext.Session.GetString("CustomerNumber");
            var orders = new List<Order>();

            await foreach (var order in _orderTableClient.QueryAsync<Order>(o => o.CustomerNumber == customerNumber))
            {
                orders.Add(order);
            }

            var orderDetails = new List<(Order order, Product product)>();
            foreach (var order in orders.OrderByDescending(o => o.OrderDate))
            {
                try
                {
                    var product = await _productTableClient.GetEntityAsync<Product>("Product", order.ProductNumber);
                    orderDetails.Add((order, product.Value));
                }
                catch
                {
                    orderDetails.Add((order, null));
                }
            }

            return View(orderDetails);
        }

        // Helper Methods
        private bool IsCustomerAuthenticated()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Customer";
        }

        private async Task UpdateCartCount()
        {
            var customerNumber = HttpContext.Session.GetString("CustomerNumber");
            if (!string.IsNullOrEmpty(customerNumber))
            {
                var cart = await _tableService.GetCustomerCartAsync(customerNumber);
                ViewBag.CartCount = cart.Count;
            }
        }

        private string GenerateOrderNumber()
        {
            long timestamp = DateTime.UtcNow.Ticks;
            string numberPart = timestamp.ToString().Substring(timestamp.ToString().Length - 10);
            return $"OR{numberPart}";
        }

        // Helper classes
        public class CartItem
        {
            public Product Product { get; set; }
            public int Quantity { get; set; }
        }

        public class CheckoutItem
        {
            public string ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
using Azure.Storage.Queues;
using CLDV6212_POE.Models;
using CLDV6212_POE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CLDV6212_POE.Controllers
{
    public class OrderController : Controller
    {
        //Table Storage Service
        private readonly TableStorageService _theTables;

        //Queue Storage Service
        private readonly QueueStorageService _theQueue;

        /// <summary>
        /// Constructor to initialize storage services
        /// </summary>
        /// <param name="tableStorage"></param>
        /// <param name="theQueue"></param>
        public OrderController(TableStorageService tableStorage, QueueStorageService theQueue)
        {
            _theTables = tableStorage;
            _theQueue = theQueue;
        }

        //Get Methods for CRUD operations
        //------------------------------------------------------------------------------------------//
        //Index method to list all orders
        public async Task<IActionResult> Index(string searchString)
        {
            // Check if user is logged in using session
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get user info from session
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("Role");

            ViewBag.UserName = userName;
            ViewBag.Role = role;

            var orders = await _theTables.GetAllOrderAsync();

            //Filter orders based on search string
            //Added Functionality for Part 2 POE
            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o =>
                    o.OrderNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    o.CustomerNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    o.ProductNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                ViewBag.SearchString = searchString;
            }

            return View(orders);
        }

        //Create method to add a new order
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        //Read method to view details of a specific order
        public async Task<IActionResult> Read(string partitionKey, string rowKey)
        {
            var order = await _theTables.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        //Update method to modify an existing order
        public async Task<IActionResult> Update(string partitionKey, string rowKey)
        {
            var order = await _theTables.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(order);
        }

        //Delete method to remove an order
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _theTables.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }
        //------------------------------------------------------------------------------------------//

        //Post Methods for CRUD operations
        //------------------------------------------------------------------------------------------//
        //Create method to add a new order
        //This method also sends a message to the queue after successful creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Convert OrderDate to UTC if it has a value
                    if (model.OrderDate.HasValue)
                    {
                        model.OrderDate = DateTime.SpecifyKind(model.OrderDate.Value, DateTimeKind.Utc);
                    }

                    //This code was helped created by stackoverflow user Luke Garrigan
                    //https://stackoverflow.com/questions/71439701/error-message-has-reached-maxdequeuecount-of-5-moving-message-to-queue-webjo
                    //Unfortunately no call services method were used as there is a queue trigger function that will process the message
                    //when it is added to the queue
                    string jsonOrder = JsonSerializer.Serialize(model);
                    var bytes = Encoding.UTF8.GetBytes(jsonOrder);
                    await _theQueue.SendOrderAsync(Convert.ToBase64String(bytes));

                    TempData["Message"] = "Order created successfully and message sent!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating order: " + ex.Message);
                }
            }

            //Repopulate dropdowns if validation fails
            await PopulateDropdowns();
            return View(model);
        }

        //Update method to modify an existing order
        //This method does not send a queue message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Order model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Convert OrderDate to UTC if it has a value
                    if (model.OrderDate.HasValue)
                    {
                        model.OrderDate = DateTime.SpecifyKind(model.OrderDate.Value, DateTimeKind.Utc);
                    }

                    await _theTables.UpdateOrderAsync(model);
                    TempData["Message"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating order: " + ex.Message);
                }
            }

            await PopulateDropdowns();
            return View(model);
        }

        //Delete method to remove an order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deletes(string partitionKey, string rowKey)
        {
            try
            {
                await _theTables.DeleteOrderAsync(partitionKey, rowKey);
                TempData["Message"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting order: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
        //------------------------------------------------------------------------------------------//
        //End of Post Methods for CRUD operations

        //------------------------------------------------------------------------------------------//
        //Added functionality for Queue Storage Service
        //Peek method to view the next message in the queue without removing it
        public async Task<IActionResult> Peek()
        {
            var msg = await _theQueue.PeekOrderAsync();
            ViewBag.PeekedMessage = msg;
            var orders = await _theTables.GetAllOrderAsync();
            return View("Index", orders);
        }

        //Helper method to populate dropdown lists
        private async Task PopulateDropdowns()
        {
            var customers = await _theTables.GetAllCustomerAsync();
            var products = await _theTables.GetAllProductAsync();

            ViewBag.CustomerList = new SelectList(customers, "RowKey", "RowKey", null);
            ViewBag.ProductList = new SelectList(products, "RowKey", "RowKey", null);
        }
        //------------------------------------------------------------------------------------------//
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
using Azure.Data.Tables;
using CLDV6212_POE.Models;
using CLDV6212_POE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Configuration;
using System.Text;

namespace CLDV6212_POE.Controllers
{
    public class CustomerController : Controller
    {
        //Table Storage Service
        private readonly TableStorageService _theTables;

        //------------------------------------------------------------------------------------------//
        private readonly AppDatabaseContext theContext;
        //------------------------------------------------------------------------------------------//

        /// <summary>
        /// Constructor to initialize storage services
        /// </summary>
        /// <param name="tableStorage"></param>
        public CustomerController(TableStorageService tableStorage, AppDatabaseContext context)
        {
            _theTables = tableStorage;
            theContext = context;
        }

        //Get Methods for CRUD operations
        //------------------------------------------------------------------------------------------//
        //Index method to list all customers
        public async Task<IActionResult> Index()
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

            var customers = await _theTables.GetAllCustomerAsync();
            return View(customers);
        }

        //Create method to add a new customer
        public ActionResult Create()
        {
            return View();
        }

        //Read method to view details of a specific customer
        public async Task<IActionResult> Read(string partitionKey, string rowKey)
        {
            var customer = await _theTables.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        //Update method to modify an existing customer
        public async Task<IActionResult> Update(string partitionKey, string rowKey)
        {
            var customer = await _theTables.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        //Delete method to remove a customer
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _theTables.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }
        //------------------------------------------------------------------------------------------//
        //End of Get Methods for CRUD operations//

        //Post Methods for CRUD operations//
        //------------------------------------------------------------------------------------------//
        //Create method to add a new customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (ModelState.IsValid)
            {
                await _theTables.CallInsertCustomerFunction(model);

                // Create user login with matching email
                User newUser = new User
                {
                    UserName = model.CustomerName,
                    UserSurname = model.CustomerSurname,
                    Email = model.CustomerEmail,  // This must match
                    Password = model.CustomerPassword,
                    Role = "Customer"
                };

                theContext.Add(newUser);
                await theContext.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        //Read method to view details of a specific customer
        public async Task<IActionResult> Reads(string partitionKey, string rowKey)
        {
            var customer = await _theTables.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Update customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Customer model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original customer data to find the original email
                    var originalCustomer = await _theTables.GetCustomerAsync(model.PartitionKey, model.RowKey);

                    // Update Azure Table Storage Customer
                    await _theTables.UpdateCustomerAsync(model);

                    // Update SQL Database User using original email
                    await UpdateSQLUserByEmail(originalCustomer.CustomerEmail, model);

                    TempData["SuccessMessage"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }

            return View(model);
        }

        // Helper method to update SQL user by original email
        private async Task UpdateSQLUserByEmail(string originalEmail, Customer updatedCustomer)
        {
            try
            {
                // Find user by the original email
                var user = await theContext.Users
                    .FirstOrDefaultAsync(u => u.Email == originalEmail && u.Role == "Customer");

                if (user != null)
                {
                    // Update all fields
                    user.UserName = updatedCustomer.CustomerName;
                    user.UserSurname = updatedCustomer.CustomerSurname;
                    user.Email = updatedCustomer.CustomerEmail;
                    user.Password = updatedCustomer.CustomerPassword;

                    theContext.Users.Update(user);
                    await theContext.SaveChangesAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No SQL user found with email: {originalEmail}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating SQL user: {ex.Message}");
                throw;
            }
        }

        // Update the Deletes method in CustomerController
        [HttpPost]
        public async Task<IActionResult> Deletes(string partitionKey, string rowKey)
        {
            try
            {
                // Get the User ID before deleting the customer
                var userId = await GetUserIdByCustomerNumber(rowKey);

                // Delete from Azure Table Storage
                await _theTables.DeleteCustomerAsync(partitionKey, rowKey);

                // Delete from SQL Database if user exists
                if (userId.HasValue)
                {
                    var user = await theContext.Users.FindAsync(userId.Value);
                    if (user != null)
                    {
                        theContext.Users.Remove(user);
                        await theContext.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Customer and associated user account deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Add this helper method to CustomerController as well
        private async Task<int?> GetUserIdByCustomerNumber(string customerNumber)
        {
            try
            {
                // Get the customer from Azure Table Storage
                var customer = await _theTables.GetCustomerAsync("Customer", customerNumber);

                if (customer?.CustomerEmail != null)
                {
                    // Find user in SQL database by email
                    var user = await theContext.Users
                        .FirstOrDefaultAsync(u => u.Email == customer.CustomerEmail);

                    return user?.Id;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user ID: {ex.Message}");
            }

            return null;
        }
        //------------------------------------------------------------------------------------------//
        //End of Post Methods for CRUD operations//
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
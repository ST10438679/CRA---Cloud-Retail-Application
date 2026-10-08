using Azure.Data.Tables;
using CLDV6212_POE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CLDV6212_POE.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User model)
        {
            ModelState.Remove("UserName");
            ModelState.Remove("UserSurname");
            ModelState.Remove("Role");
            ModelState.Remove("Id");

            if (ModelState.IsValid)
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Id, Email, UserName, UserSurname, Role FROM Users WHERE Email = @Email AND Password = @Password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", model.Email);
                        command.Parameters.AddWithValue("@Password", model.Password);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Store basic user info
                                HttpContext.Session.SetInt32("UserId", reader.GetInt32(0));
                                HttpContext.Session.SetString("Email", reader.GetString(1));
                                HttpContext.Session.SetString("UserName", reader.GetString(2));
                                HttpContext.Session.SetString("UserSurname", reader.GetString(3));
                                HttpContext.Session.SetString("Role", reader.GetString(4));

                                string role = reader.GetString(4);

                                // For customers, get their CustomerNumber from Azure Table Storage
                                if (role == "Customer")
                                {
                                    string email = reader.GetString(1);

                                    // Get customer details from Azure Table Storage
                                    var customerNumber = await GetCustomerNumberByEmail(email);

                                    if (!string.IsNullOrEmpty(customerNumber))
                                    {
                                        HttpContext.Session.SetString("CustomerNumber", customerNumber);
                                        return RedirectToAction("Index", "User");
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, "Customer record not found.");
                                        return View(model);
                                    }
                                }
                                else if (role == "Admin")
                                {
                                    return RedirectToAction("Index", "Home");
                                }
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                            }
                        }
                    }
                }
            }

            return View(model);
        }

        // Add this helper method to AccountController
        private async Task<string> GetCustomerNumberByEmail(string email)
        {
            try
            {
                var connectionString = _configuration["AzureStorage:ConnectionString"];
                var serviceClient = new TableServiceClient(connectionString);
                var customerTableClient = serviceClient.GetTableClient("CustomerTable");

                await foreach (var customer in customerTableClient.QueryAsync<Customer>(c => c.CustomerEmail == email))
                {
                    return customer.CustomerNumber;
                }
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error getting customer number: {ex.Message}");
            }

            return null;
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
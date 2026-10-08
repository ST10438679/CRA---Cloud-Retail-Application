using CLDV6212_POE.Models;
using CLDV6212_POE.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CLDV6212_POE.Controllers
{
    public class ContractsController : Controller
    {
        //File Share Storage Service and Table Storage Service
        //------------------------------------------------------------------------------------------//
        private readonly FileShareStorageService _fileService;
        private readonly TableStorageService _tableService;
        //------------------------------------------------------------------------------------------//

        /// <summary>
        /// Constructor to initialize storage services
        /// </summary>
        /// <param name="fileService"></param>
        /// <param name="tableService"></param>
        public ContractsController(FileShareStorageService fileService, TableStorageService tableService)
        {
            _fileService = fileService;
            _tableService = tableService;
        }

        //Get Methods for CRUD operations
        //------------------------------------------------------------------------------------------//
        //Index method to list all contracts with customer info
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

            var model = new Contracts
            {
                Customers = await _tableService.GetAllCustomerAsync(),
                UploadedFiles = await _fileService.ListFilesWithCustomerInfoAsync()
            };
            return View(model);
        }
        //------------------------------------------------------------------------------------------//
        //End of Get Methods for CRUD operations

        //Download method to download a specific file
        public async Task<IActionResult> Download(string fileName)
        {
            try
            {
                var fileStream = await _fileService.DownloadFileAsync(fileName);
                var originalFileName = fileName.Contains("_") ? fileName.Substring(fileName.IndexOf("_") + 1) : fileName;
                return File(fileStream, "application/octet-stream", originalFileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error downloading file: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        //Post methods for CRUD operations
        //------------------------------------------------------------------------------------------//
        //Delete method to remove a specific file
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            try
            {
                await _fileService.DeleteFileAsync(fileName);
                TempData["Success"] = "File deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        //Upload method to upload a new contract file
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string customerId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(customerId))
            {
                TempData["Error"] = "Please select a customer.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                //Verify customer exists
                var customer = await _tableService.GetCustomerAsync("Customer", customerId);
                if (customer == null)
                {
                    TempData["Error"] = "Selected customer does not exist.";
                    return RedirectToAction(nameof(Index));
                }

                //Upload file with customer ID prefix
                //Old Code
                //await _fileService.UploadFileWithCustomerIdAsync(file, customerId);

                //New Code to call Azure Function
                await _fileService.CallFileShareStorageFunction(file, customerId);

                TempData["Success"] = $"File '{file.FileName}' uploaded successfully for customer {customerId}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error uploading file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
        //------------------------------------------------------------------------------------------//
        //End of Post Methods for CRUD operations
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
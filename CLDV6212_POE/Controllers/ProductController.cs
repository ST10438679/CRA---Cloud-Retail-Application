using CLDV6212_POE.Models;
using CLDV6212_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace CLDV6212_POE.Controllers
{
    public class ProductController : Controller
    {
        //Table Storage Service
        private readonly TableStorageService _theTables;
        //Blob Storage Service
        private readonly BlobStorageService _theBlobs;

        /// <summary>
        /// Constructor to initialize storage services
        /// </summary>
        /// <param name="tableStorage"></param>
        public ProductController(TableStorageService tableStorage, BlobStorageService theBlobs)
        {
            _theTables = tableStorage;
            _theBlobs = theBlobs;
        }

        //Get Methods for CRUD operations
        //------------------------------------------------------------------------------------------//
        //Index method to list all products
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

            var products = await _theTables.GetAllProductAsync();
            return View(products);
        }

        //Create method to add a new product
        public ActionResult Create()
        {
            return View();
        }

        //Read method to view details of a specific product
        public async Task<IActionResult> Read(string partitionKey, string rowKey)
        {
            var product = await _theTables.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        //Update method to modify an existing product
        public async Task<IActionResult> Update(string partitionKey, string rowKey)
        {
            var product = await _theTables.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        //Delete method to remove a product
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var product = await _theTables.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
        //------------------------------------------------------------------------------------------//
        //End of Get Methods for CRUD operations//

        //Post Methods for CRUD operations
        //------------------------------------------------------------------------------------------//
        //Create method to add a new product
        /// <summary>
        /// This method handles the creation of a new product, including uploading image and video files to Azure Blob Storage
        /// THis method also has additional validation and error handling to ensure robust file uploads
        /// Helped created by Claude AI
        /// Link: https://claude.ai/
        /// </summary>
        /// <param name="model"></param>
        /// <param name="imagefile"></param>
        /// <param name="videofile"></param>
        /// <param name="blobService"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile imagefile, IFormFile videofile)
        {
            // Image file validation
            if (imagefile == null || imagefile.Length == 0)
            {
                ViewData["ImageFileError"] = "Please upload an image file.";
                return View(model);
            }

            var allowedImageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/pjpeg" };
            if (!allowedImageTypes.Contains(imagefile.ContentType?.ToLower()))
            {
                ViewData["ImageFileError"] = $"Invalid image format: {imagefile.ContentType}. Only JPEG and PNG images are allowed.";
                return View(model);
            }

            var imageExtension = Path.GetExtension(imagefile.FileName)?.ToLower();
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedImageExtensions.Contains(imageExtension))
            {
                ViewData["ImageFileError"] = $"Invalid file extension: {imageExtension}. Only .jpg, .jpeg, and .png files are allowed.";
                return View(model);
            }

            // Video file validation (optional)
            if (videofile != null && videofile.Length > 0)
            {
                if (videofile.Length > 100 * 1024 * 1024)
                {
                    ViewData["VideoFileError"] = "Video file size cannot exceed 100MB.";
                    return View(model);
                }

                var allowedVideoTypes = new[] {
            "video/mp4", "video/webm", "video/ogg",
            "video/avi", "video/quicktime", "video/x-msvideo"
        };

                if (!allowedVideoTypes.Contains(videofile.ContentType?.ToLower()))
                {
                    var videoExtension = Path.GetExtension(videofile.FileName)?.ToLower();
                    var allowedVideoExtensions = new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" };

                    if (!allowedVideoExtensions.Contains(videoExtension))
                    {
                        ViewData["VideoFileError"] = $"Invalid video format: {videofile.ContentType} ({videoExtension}). Only MP4, WebM, OGG, AVI, and MOV video files are allowed.";
                        return View(model);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Upload image via Azure Function
                    //New code to call Azure Function for image upload
                    System.Diagnostics.Debug.WriteLine("Attempting to upload image via Azure Function...");
                    string imageUrl = await _theBlobs.CallImageUploadFunction(imagefile);
                    System.Diagnostics.Debug.WriteLine($"Image uploaded successfully: {imageUrl}");
                    model.ImageURL = imageUrl;

                    // Upload video via Azure Function if provided
                    //New code to call Azure Function for video upload
                    if (videofile != null && videofile.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Attempting to upload video via Azure Function...");
                        string videoUrl = await  _theBlobs.CallVideoUploadFunction(videofile);
                        System.Diagnostics.Debug.WriteLine($"Video uploaded successfully: {videoUrl}");
                        model.VideoURL = videoUrl;
                    }
                    else
                    {
                        model.VideoURL = null;
                    }

                    System.Diagnostics.Debug.WriteLine("Attempting to insert product...");
                    await _theTables.InsertProductAsync(model);
                    System.Diagnostics.Debug.WriteLine("Product inserted successfully");

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in Create: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                    ViewData["GeneralError"] = $"An error occurred while uploading files: {ex.Message}";
                    return View(model);
                }
            }

            return View(model);
        }

        //Read method to view details of a specific product
        [HttpPost]
        public async Task<IActionResult> Reads(string partitionKey, string rowKey)
        {
            var product = await _theTables.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        //Update method to modify an existing product
        /// <summary>
        /// This method handles updating an existing product, including optional updates to image and video files
        /// This method includes validation and error handling for file uploads
        /// Helped created by Claude AI
        /// Link: https://claude.ai/
        /// </summary>
        /// <param name="model"></param>
        /// <param name="imagefile"></param>
        /// <param name="videofile"></param>
        /// <param name="blobService"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Product model, IFormFile imagefile, IFormFile videofile, [FromServices] BlobStorageService blobService)
        {
            //Get existing product using the model's PartitionKey and RowKey
            var existingProduct = await _theTables.GetProductAsync(model.PartitionKey, model.RowKey);
            if (existingProduct == null)
            {
                return NotFound();
            }

            //Preserve the ETag and Timestamp from the existing product
            model.ETag = existingProduct.ETag;
            model.Timestamp = existingProduct.Timestamp;

            //Handle image file update
            if (imagefile != null && imagefile.Length > 0)
            {
                //Image validation
                var allowedImageTypes = new[] {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/pjpeg"
        };

                if (!allowedImageTypes.Contains(imagefile.ContentType?.ToLower()))
                {
                    ViewData["ImageFileError"] = $"Invalid image format: {imagefile.ContentType}. Only JPEG and PNG images are allowed.";
                    return View(model);
                }

                try
                {
                    string imageUrl = await blobService.UploadFileAsyncImage(imagefile);
                    model.ImageURL = imageUrl;
                }
                catch (Exception ex)
                {
                    ViewData["ImageFileError"] = "Error uploading image. Please try again.";
                    return View(model);
                }
            }
            else
            {
                //Keep existing image if no new image uploaded
                model.ImageURL = existingProduct.ImageURL;
            }

            //Handle video file update
            if (videofile != null && videofile.Length > 0)
            {
                //Check file size (100MB limit)
                if (videofile.Length > 100 * 1024 * 1024)
                {
                    ViewData["VideoFileError"] = "Video file size cannot exceed 100MB.";
                    return View(model);
                }

                //Check video file type
                var allowedVideoTypes = new[] {
            "video/mp4",
            "video/webm",
            "video/ogg",
            "video/avi",
            "video/quicktime",
            "video/x-msvideo"
        };

                var videoExtension = Path.GetExtension(videofile.FileName)?.ToLower();
                var allowedVideoExtensions = new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" };

                if (!allowedVideoTypes.Contains(videofile.ContentType?.ToLower()) &&
                    !allowedVideoExtensions.Contains(videoExtension))
                {
                    ViewData["VideoFileError"] = $"Invalid video format. Only MP4, WebM, OGG, AVI, and MOV video files are allowed.";
                    return View(model);
                }

                try
                {
                    string videoUrl = await blobService.UploadFileAsyncVideo(videofile);
                    model.VideoURL = videoUrl;
                }
                catch (Exception ex)
                {
                    ViewData["VideoFileError"] = "Error uploading video. Please try again.";
                    return View(model);
                }
            }
            else
            {
                //Keep existing video if no new video uploaded
                model.VideoURL = existingProduct.VideoURL;
            }

            //Remove ModelState validation errors for file uploads since they're handled above
            ModelState.Remove("imagefile");
            ModelState.Remove("videofile");

            if (ModelState.IsValid)
            {
                try
                {
                    await _theTables.UpdateProductAsync(model);
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Update error: {ex.Message}");
                    ViewData["GeneralError"] = "An error occurred while updating the product. Please try again.";
                    return View(model);
                }
            }
            else
            {
                //Debug ModelState errors
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"ModelState Error: {modelError.ErrorMessage}");
                }
            }

            return View(model);
        }

        //Delete method to remove a product
        [HttpPost]
        public async Task<IActionResult> Deletes(string partitionKey, string rowKey)
        {
            await _theTables.DeleteProductAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
        //------------------------------------------------------------------------------------------//
        //End of Post Methods for CRUD operations//
    }
    //------------------------------------------------------------------------------------------//
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CLDV6212_POE_Part_2_Functions;

public class BlobStorageFunction
{
    private readonly ILogger<BlobStorageFunction> _logger;

    public BlobStorageFunction(ILogger<BlobStorageFunction> logger)
    {
        _logger = logger;
    }

    [Function("UploadImageToBlob")]
    public async Task<IActionResult> UploadImage(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Image upload request received.");

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var containerName = Environment.GetEnvironmentVariable("ImageContainerName") ?? "images";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("AzureWebJobsStorage is not configured");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            // Check if file exists
            if (!req.HasFormContentType || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "No file was uploaded."
                });
            }

            var file = req.Form.Files[0];

            if (file.Length == 0)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "The uploaded file is empty."
                });
            }

            // Validate image file type
            var allowedImageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/pjpeg" };
            if (!allowedImageTypes.Contains(file.ContentType?.ToLower()))
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = $"Invalid image format: {file.ContentType}. Only JPEG and PNG images are allowed."
                });
            }

            // Upload to blob storage
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            var imageUrl = blobClient.Uri.ToString();
            _logger.LogInformation($"Image uploaded successfully: {imageUrl}");

            return new OkObjectResult(new
            {
                success = true,
                message = "Image uploaded successfully.",
                imageUrl = imageUrl,
                fileName = uniqueFileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to Azure Blob Storage.");
            return new ObjectResult(new
            {
                success = false,
                message = $"Error uploading image: {ex.Message}"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("UploadVideoToBlob")]
    public async Task<IActionResult> UploadVideo(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Video upload request received.");

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var containerName = Environment.GetEnvironmentVariable("VideoContainerName") ?? "videos";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("AzureWebJobsStorage is not configured");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            // Check if file exists
            if (!req.HasFormContentType || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "No file was uploaded."
                });
            }

            var file = req.Form.Files[0];

            if (file.Length == 0)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "The uploaded file is empty."
                });
            }

            // Check file size (100MB limit)
            if (file.Length > 100 * 1024 * 1024)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Video file size cannot exceed 100MB."
                });
            }

            // Validate video file type
            var allowedVideoTypes = new[] {
                "video/mp4", "video/webm", "video/ogg",
                "video/avi", "video/quicktime", "video/x-msvideo"
            };

            var videoExtension = Path.GetExtension(file.FileName)?.ToLower();
            var allowedVideoExtensions = new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" };

            if (!allowedVideoTypes.Contains(file.ContentType?.ToLower()) &&
                !allowedVideoExtensions.Contains(videoExtension))
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = $"Invalid video format. Only MP4, WebM, OGG, AVI, and MOV video files are allowed."
                });
            }

            // Upload to blob storage
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            var videoUrl = blobClient.Uri.ToString();
            _logger.LogInformation($"Video uploaded successfully: {videoUrl}");

            return new OkObjectResult(new
            {
                success = true,
                message = "Video uploaded successfully.",
                videoUrl = videoUrl,
                fileName = uniqueFileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video to Azure Blob Storage.");
            return new ObjectResult(new
            {
                success = false,
                message = $"Error uploading video: {ex.Message}"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
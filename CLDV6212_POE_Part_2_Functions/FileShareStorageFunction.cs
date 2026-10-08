using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.Shares;
using System.IO;
using System.Threading.Tasks;

namespace CLDV6212_POE_Part_2_Functions;

public class FileShareStorageFunction
{
    private readonly ILogger<FileShareStorageFunction> _logger;
    private readonly string _connectionString;
    private readonly string _fileShareName = "dummycontracts";
    private readonly ShareClient _shareClient;

    public FileShareStorageFunction(ILogger<FileShareStorageFunction> logger)
    {
        _logger = logger;

        // Get connection string from environment variables
        _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured");

        _shareClient = new ShareClient(_connectionString, _fileShareName);
    }

    [Function("UploadFileToShare")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("File upload request received.");

        try
        {
            // Check if the request contains a file
            if (!req.HasFormContentType || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "No file was uploaded. Please include a file in the request."
                });
            }

            var file = req.Form.Files[0];
            var customerId = req.Form["customerId"].ToString();

            if (file.Length == 0)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "The uploaded file is empty."
                });
            }

            // Get the root directory of the file share
            var directory = _shareClient.GetRootDirectoryClient();

            // Create filename with customer ID prefix if provided
            var fileName = string.IsNullOrEmpty(customerId)
                ? file.FileName
                : $"{customerId}_{file.FileName}";

            var fileClient = directory.GetFileClient(fileName);

            // Upload the file
            using (var stream = file.OpenReadStream())
            {
                await fileClient.CreateAsync(file.Length);
                await fileClient.UploadAsync(stream);
            }

            _logger.LogInformation($"File '{fileName}' uploaded successfully.");

            return new OkObjectResult(new
            {
                success = true,
                message = $"File '{file.FileName}' uploaded successfully.",
                fileName = fileName,
                fileSize = file.Length,
                customerId = customerId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Azure File Share.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
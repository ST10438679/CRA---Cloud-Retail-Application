using Azure.Storage.Blobs;

namespace CLDV6212_POE.Services
{
    public class BlobStorageService
    {
        //------------------------------------------------------------------------------------------//
        //Read only holders of the keys that connect to the azure storage and their containers
        private readonly string _connectionString;
        private readonly string _imageContainerName;
        private readonly string _videoContainerName;
        private readonly string _imageFunctionUrl;
        private readonly string _videoFunctionUrl;
        //------------------------------------------------------------------------------------------//

        //------------------------------------------------------------------------------------------//
        //Connecting our holders to azure via json file
        public BlobStorageService(IConfiguration config)
        {
            _connectionString = config["AzureStorage:ConnectionString"];
            _imageContainerName = config["AzureStorage:ContainerName"];
            _videoContainerName = config["AzureStorage:SecondContainerName"];
            _imageFunctionUrl = config["AzureFunctions:UploadImageToBlobUrl"];
            _videoFunctionUrl = config["AzureFunctions:UploadVideoToBlobUrl"];
        }
        //------------------------------------------------------------------------------------------//

        //------------------------------------------------------------------------------------------//
        //This method is uploading the image given to blob storage and return a url string 
        //This code was helped created by IIEVC School of Computer Science youtube channel
        //Link: "https://www.youtube.com/watch?v=lQ7VGslAVrU&list=PL480DYS-b_kevhFsiTpPIB2RzhKPig4iK&index=10"
        //The video is unlisted
        public async Task<string> UploadFileAsyncImage(IFormFile file)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_imageContainerName);

            await containerClient.CreateIfNotExistsAsync();

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return blobClient.Uri.ToString();
        }
        //------------------------------------------------------------------------------------------//

        //------------------------------------------------------------------------------------------//
        //This method is a copy of method above just created for the event instead of venue
        //This code was helped created by IIEVC School of Computer Science youtube channel
        //Link: "https://www.youtube.com/watch?v=lQ7VGslAVrU&list=PL480DYS-b_kevhFsiTpPIB2RzhKPig4iK&index=10"
        //The video is unlisted
        public async Task<string> UploadFileAsyncVideo(IFormFile file)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var SecondContainerClient = blobServiceClient.GetBlobContainerClient(_videoContainerName);

            await SecondContainerClient.CreateIfNotExistsAsync();

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = SecondContainerClient.GetBlobClient(uniqueFileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return blobClient.Uri.ToString();
        }
        //------------------------------------------------------------------------------------------//

        //------------------------------------------------------------------------------------------//
        //Azure function caller methods
        public async Task<string> CallImageUploadFunction(IFormFile file)
        {
            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();

            // Add the file
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            // Azure Function URL
            var functionUrl = _imageFunctionUrl;

            var response = await httpClient.PostAsync(functionUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<BlobUploadResponse>(responseContent);

            return result?.imageUrl ?? throw new Exception("Failed to get image URL from response");
        }

        public async Task<string> CallVideoUploadFunction(IFormFile file)
        {
            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();

            // Add the file
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            // Azure Function URL
            var functionUrl = _videoFunctionUrl;

            var response = await httpClient.PostAsync(functionUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<BlobUploadResponse>(responseContent);

            return result?.videoUrl ?? throw new Exception("Failed to get video URL from response");
        }

        // Response model for deserialization
        private class BlobUploadResponse
        {
            public bool success { get; set; }
            public string message { get; set; }
            public string imageUrl { get; set; }
            public string videoUrl { get; set; }
            public string fileName { get; set; }
        }
        //------------------------------------------------------------------------------------------//
    }
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
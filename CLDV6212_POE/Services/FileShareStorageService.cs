using Azure.Storage.Files.Shares;
using CLDV6212_POE.Models;

namespace CLDV6212_POE.Services
{
    public class FileShareStorageService
    {
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// Our read only holder that connects to the azure storage file share
        /// </summary>
        private readonly ShareClient _shareClient;
        private readonly TableStorageService _tableService;
        private readonly string _uploadFunctionUrl;
        //------------------------------------------------------------------------------------------//

        /// <summary>
        /// Constructor to connect our holder to azure via json file
        /// </summary>
        /// <param name="config"></param>
        /// <param name="tableService"></param>
        public FileShareStorageService(IConfiguration config, TableStorageService tableService)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            var fileshareName = "dummycontracts";
            _shareClient = new ShareClient(connectionString, fileshareName);
            _shareClient.CreateIfNotExists();
            _tableService = tableService;
            _uploadFunctionUrl = config["AzureFunctions:UploadFileToShareUrl"];
        }

        //This code was helped created by IIEVC School of Computer Science youtube channel
        //Link: https://youtube.com/playlist?list=PL480DYS-b_kcZiyuCyHolh6Nad8J_Xnk7&si=b7ghiNAQ1j_vConX
        //This video is unlisted
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// This method uploads a file to the Azure File Share
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return;

            var directory = _shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(file.FileName);

            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadAsync(stream);
        }

        /// <summary>
        /// This method lists all files in the Azure File Share
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> ListFileAsync()
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var files = new List<string>();

            await foreach (var item in directory.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                    files.Add(item.Name);
            }

            return files;
        }

        /// <summary>
        /// This method downloads a file from the Azure File Share
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(fileName);
            var download = await fileClient.DownloadAsync();
            return download.Value.Content;
        }
        //------------------------------------------------------------------------------------------//

        //This is new code added with a basis from the above methods
        //THis code was helped created by Claude AI
        //Link: https://claude.ai/
        //------------------------------------------------------------------------------------------//
        /// <summary>
        /// This method deletes a file from the Azure File Share
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task DeleteFileAsync(string fileName)
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(fileName);
            await fileClient.DeleteAsync();
        }

        /// <summary>
        /// This method uploads a file to the Azure File Share and associates it with a customer ID
        /// </summary>
        /// <param name="file"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        public async Task UploadFileWithCustomerIdAsync(IFormFile file, string customerId)
        {
            if (file == null || file.Length == 0)
                return;

            var directory = _shareClient.GetRootDirectoryClient();
            // Prefix filename with customer ID to associate it
            var fileName = $"{customerId}_{file.FileName}";
            var fileClient = directory.GetFileClient(fileName);

            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadAsync(stream);
        }

        /// <summary>
        /// This method lists all files in the Azure File Share along with associated customer information
        /// </summary>
        /// <returns></returns>
        public async Task<List<theFileInfo>> ListFilesWithCustomerInfoAsync()
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var fileInfos = new List<theFileInfo>();
            var customers = await _tableService.GetAllCustomerAsync();

            await foreach (var item in directory.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    var fileInfo = new theFileInfo
                    {
                        FileName = item.Name,
                        UploadDate = DateTime.Now
                    };

                    if (item.Name.Contains("_"))
                    {
                        var parts = item.Name.Split('_', 2);
                        var customerId = parts[0];
                        var originalFileName = parts[1];

                        var customer = customers.FirstOrDefault(c => c.CustomerNumber == customerId);
                        if (customer != null)
                        {
                            fileInfo.CustomerId = customerId;
                            fileInfo.CustomerName = $"{customer.CustomerName} {customer.CustomerSurname}";
                            fileInfo.OriginalFileName = originalFileName;
                        }
                        else
                        {
                            fileInfo.OriginalFileName = item.Name;
                        }
                    }
                    else
                    {
                        fileInfo.OriginalFileName = item.Name;
                    }

                    fileInfos.Add(fileInfo);
                }
            }

            return fileInfos;
        }
        //------------------------------------------------------------------------------------------//

        //------------------------------------------//
        //This is new code added with a basis from the above methods
        //For Part 2
        public async Task CallFileShareStorageFunction(IFormFile file, string customerId)
        {
            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();

            // Add the file
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            // Add customer ID
            content.Add(new StringContent(customerId), "customerId");

            // Azure Function URL
            var functionUrl = _uploadFunctionUrl;

            var response = await httpClient.PostAsync(functionUrl, content);
            response.EnsureSuccessStatusCode();
        }
        //------------------------------------------------------------------------------------------//
    }
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//
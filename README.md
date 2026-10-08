# Cloud Retail Application (CRA)
**CLDV6212 POE Part 3 | ST10438679**

An e-commerce style web application (customers, products, orders, cart and contracts) built on **ASP.NET Core MVC (.NET 8)** and the **Azure** platform. A separate **Azure Functions** app handles uploads and queue work.

## Highlights
- **Four Azure Storage services**, each wrapped in its own service class and registered through dependency injection: Table (`TableStorageService`), Blob (`BlobStorageService`), Queue (`QueueStorageService`) and File Share (`FileShareStorageService`).
- **Azure Functions** (.NET 8 isolated worker, Application Insights): `InsertCustomerToTable`, `UploadImageToBlob`, `UploadVideoToBlob`, `UploadFileToShare` and a queue sender.
- **Azure SQL Database** through Entity Framework Core for users, products, orders, carts and contracts.
- **Admin and customer roles**, session-based login, and views per feature (Account, Customer, Product, Order, Contracts, User).
- Hosted on **Azure App Service**.

## Tech stack
C# · .NET 8 · ASP.NET Core MVC · Entity Framework Core · Azure SQL · Azure Table / Blob / Queue / File Share Storage · Azure Functions · Application Insights · Bootstrap

## Solution structure
```
CLDV6212_POE.sln
├── CLDV6212_POE/                       # MVC web app (Controllers, Models, Services, Views)
└── CLDV6212_POE_Part_2_Functions/      # Azure Functions app
```

## Running it yourself
You need your own Azure resources, since the original ones are not included.

1. Create an Azure SQL database and an Azure Storage account.
2. In `CLDV6212_POE/appsettings.json`, replace the `<placeholder>` values for `ConnectionStrings:DefaultConnection`, `AzureStorage:ConnectionString` and the four `AzureFunctions:*Url` entries (your deployed Function URLs including their function keys). Better still, keep them out of the file with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or environment variables (`ConnectionStrings__DefaultConnection`, `AzureStorage__ConnectionString`, `AzureFunctions__UploadImageToBlobUrl`, ...).
3. Copy `CLDV6212_POE_Part_2_Functions/local.settings.example.json` to `local.settings.json` (git-ignored) and fill in your storage connection string.
4. Apply the database: `dotnet ef database update --project CLDV6212_POE`.
5. Run the web app and the Functions project.

> No real keys or passwords are stored in this repository.

## Demos
- [Application showcase (YouTube)](https://youtu.be/wpI3cz68bWg)
- [Azure services showcase (YouTube)](https://youtu.be/51V5zoElSwY)

The assessment write-up is included as `ST10438679_CLDV6212_POE_Final_Part.pdf` / `.docx`.

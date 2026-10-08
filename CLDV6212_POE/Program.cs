using CLDV6212_POE.Models;
using CLDV6212_POE.Services;
using Microsoft.EntityFrameworkCore;

namespace CLDV6212_POE
{
    //Part 3 Final POE
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            //------------------------------------------------------------------------------------------//
            //Our Blob stprage, Queues, Tables and Fileshares services
            builder.Services.AddSingleton<TableStorageService>();
            builder.Services.AddSingleton<QueueStorageService>();
            builder.Services.AddSingleton<FileShareStorageService>();
            builder.Services.AddSingleton<BlobStorageService>();
            //------------------------------------------------------------------------------------------//

            // Add Session support
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //------------------------------------------------------------------------------------------//
            builder.Services.AddDbContext<AppDatabaseContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            //------------------------------------------------------------------------------------------//

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // CRITICAL: Add UseSession() BEFORE UseAuthorization()
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}

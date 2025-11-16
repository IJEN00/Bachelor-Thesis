using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.Services.Suppliers;
using InventoryApp.Services.Suppliers.Mouser;
using InventoryApp.Services.Suppliers.TME;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IComponentService, ComponentService>();
            builder.Services.AddScoped<ILocationService, LocationService>();
            builder.Services.AddScoped<IDocumentService, DocumentService>();

            builder.Services.AddScoped<IProjectPlanningService, ProjectPlanningService>();

            builder.Services.Configure<TMEApiOptions>(builder.Configuration.GetSection("TMEApi"));
            builder.Services.Configure<MouserApiOptions>(builder.Configuration.GetSection("MouserApi"));

            builder.Services.AddHttpClient<TMEApiClient>();
            builder.Services.AddHttpClient<MouserApiClient>();

            builder.Services.AddScoped<ISupplierClient, TMEApiClient>();
            builder.Services.AddScoped<ISupplierClient, MouserApiClient>();
            builder.Services.AddScoped<SupplierAggregatorService>();           

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

using BookingCinema.Data;          // <-- Apna DbContext namespace
using Microsoft.EntityFrameworkCore;

namespace B_Cinema
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================
            // 1️⃣ Services
            // =========================

            // MVC
            builder.Services.AddControllersWithViews();

            // DbContext (SQL Server)
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
            );

            // Session (Authentication)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            // Add this in ConfigureServices
            builder.Services.AddSession();
            builder.Services.AddHttpContextAccessor();
            var app = builder.Build();

            // =========================
            // 2️⃣ Middleware
            // =========================

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
           

            // In Configure (or after app.UseRouting())
            app.UseSession();


            // ⚠️ Session MUST be before Authorization
            app.UseSession();

            app.UseAuthorization();


            // =========================
            // 3️⃣ Routing
            // =========================

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}"
            );

            app.Run();
        }
    }
}

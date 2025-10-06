using System.Globalization;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Http.Features;


            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //Azure Functions
            builder.Services.AddHttpClient("Functions", (sp, client) =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var baseUrl = cfg["Functions:BaseUrl"] ?? throw new InvalidOperationException("Functions:BaseUrl missing");
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/"); // adjust if your Functions don't use /api
                client.Timeout = TimeSpan.FromSeconds(100);
            });

            //Register Azure Function Service
            builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
            });

            //Add logging
            builder.Services.AddLogging();

            var app = builder.Build();

            //Set culture for decimal handling (FIXES PRICE ISSUE)
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();

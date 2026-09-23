using Gatepaswebapi.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Odbc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Gatepaswebapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();

         

            var builder = WebApplication.CreateBuilder(args);
            // Configure Kestrel server options
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestLineSize = 8192; // Increase query string size limit
                options.Limits.MaxRequestBodySize = 104857600; // Increase request body size
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithOrigins("http://10.80.1.9", "https://10.80.1.9", "http://10.80.1.80", "http://localhost:5173"));
               
            });
           

            builder.Services.AddScoped<SMSService>();// ADD SMSERVICES CREATED BY DEVELOPER
       
            var app = builder.Build();

                   // =====================================================
                    // PRODUCTION ERROR HANDLING
                    // =====================================================

                    if (!app.Environment.IsDevelopment())
                    {

                            app.UseSwagger();
                            app.UseSwaggerUI();

                        app.UseExceptionHandler(errorApp =>
                        {
                            errorApp.Run(async context =>
                            {
                                context.Response.StatusCode =
                                    StatusCodes.Status500InternalServerError;

                                context.Response.ContentType =
                                    "application/json";

                                await context.Response.WriteAsJsonAsync(new
                                {
                                    success = false,
                                    message = "An unexpected error occurred."
                                });
                            });
                        });
                    }

                    // =====================================================
                    // SECURITY HEADERS
                    // =====================================================

                    app.Use(async (context, next) =>
                    {
                        context.Response.Headers["X-Content-Type-Options"] =
                            "nosniff";

                        context.Response.Headers["Referrer-Policy"] =
                            "strict-origin-when-cross-origin";

                        context.Response.Headers["Content-Security-Policy"] =
                            "default-src 'self'; base-uri 'self';";

                        context.Response.Headers["Cache-Control"] =
                            "no-store, no-cache, must-revalidate, max-age=0";

                        context.Response.Headers["Pragma"] =
                            "no-cache";

                        context.Response.Headers["Expires"] =
                            "0";

                        await next();
                    });
                    


            app.UseCors("AllowAll"); //Enable CORS (If Calling from Browser
            app.UseAuthorization();
            app.MapControllers();


            app.Run();

            

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
     
        
    }

}

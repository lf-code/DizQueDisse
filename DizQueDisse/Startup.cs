using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DizQueDisse.Data;
using Microsoft.EntityFrameworkCore;
using DizQueDisse.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using DizQueDisse.Models;

namespace DizQueDisse
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //Add DbContext:
            services.AddDbContext<MyDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("MyDefaultConnection")));

            //Add and configure Identity:
            services.AddIdentity<MyUser, IdentityRole>()
                    .AddEntityFrameworkStores<MyDbContext>()
                    .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 2;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.LoginPath = "/Conta";
                options.AccessDeniedPath = "/Conta";
                options.SlidingExpiration = true;
            });

            //Add MVC
            services.AddMvc();

            //Add my singleton services:
            services.AddSingleton<TwitterService>();
            services.AddSingleton<QuoteService>();
            services.AddSingleton<WeatherService>();
            services.AddSingleton<IPMAService>();
            services.AddSingleton<AppService>();

            /* !!IMPORTANT!!*/
            /*https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/background-tasks-with-ihostedservice#deployment-considerations-and-takeaways */
            //check APP POOL Recycling settings on ISS on VM!!

            services.AddSingleton<IHostedService, MainTimerService>();

            //Singleton objects are the same for every object and every request
            //Scoped objects are the same within a request, but different across different requests
            //Transient objects are always different; a new instance is provided to every controller and every service.
        }


        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //Handle Errors:
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            //Authentication manager
            app.UseAuthentication();

            //Serve js, css, images...
            app.UseStaticFiles();

            //Use MVC with attribute routing
            app.UseMvc();

        }
    }
}

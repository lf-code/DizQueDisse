using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using DizQueDisse.Data;
using DizQueDisse.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Console;
using DizQueDisse.Secrets;

namespace DizQueDisse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            //Seeder.SeedDatabase(host);

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args) //Already configures loggers!
            .UseStartup<Startup>()
            .ConfigureLogging((hostingContext, logging) => { //Configure loggers!
                string fileName = hostingContext.HostingEnvironment.EnvironmentName == "Production" ? "DizQueDisse" : "DizQueDisseDebug";
                StreamWriter sw = new StreamWriter(hostingContext.HostingEnvironment.ContentRootPath + $"/logs/{fileName}.log",true);
                sw.AutoFlush = true;
                Console.SetOut(sw);

                logging.ClearProviders();
                logging.AddConsole();
                logging.AddFilter<ConsoleLoggerProvider>("Microsoft", LogLevel.Warning);
                logging.AddFilter<ConsoleLoggerProvider>("DizQueDisse", LogLevel.Information);
            })
            .Build();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackUpCollectionDAL.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackUpCollectionServiceDelay
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    string conString = Microsoft
                       .Extensions
                       .Configuration
                       .ConfigurationExtensions
                       .GetConnectionString(configuration, "DefaultConnection");

                    

                    services.AddHostedService<Worker>()
                    .AddDbContext<CoreDbContext>(x => x.UseSqlServer(conString));

                }
                );
    }
}

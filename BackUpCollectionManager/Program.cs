using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BackUpCollectionDAL.DataBase;
using Microsoft.EntityFrameworkCore;

namespace BackUpCollectionManager
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

                    string conString =
                       ConfigurationExtensions
                       .GetConnectionString(configuration, "DefaultConnection");

                    services.AddHostedService<Worker>()
                    .AddDbContext<CoreDbContext>(x => x.UseSqlServer(conString));
                });
    }
}

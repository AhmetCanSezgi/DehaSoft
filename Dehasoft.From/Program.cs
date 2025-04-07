using Dehasoft.Business.Services;
using Dehasoft.DataAccess.Repositories;
using Dehasoft.WinForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Dehasoft.Business.Mappings; // AutoMapper ekleniyor

namespace Dehasoft
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.AddAppServices(config);
          

            var serviceProvider = services.BuildServiceProvider();

            ApplicationConfiguration.Initialize();

            var orderService = serviceProvider.GetRequiredService<IOrderService>();
            var productRepository = serviceProvider.GetRequiredService<IProductRepository>();
            var ProductService = serviceProvider.GetRequiredService<IProductService>();
            

            Application.Run(new Form1(orderService, productRepository, ProductService));
        }
    }
}

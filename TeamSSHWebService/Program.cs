using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace TeamSSHWebService
{
    public class Program
    {
        #region Public Methods

        public static void Main()
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();
            host.Run();
        }

        #endregion
    }
}
